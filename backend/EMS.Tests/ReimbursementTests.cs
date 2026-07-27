using EMS.Application.Features.Payroll.Handlers;
using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Features.Reimbursements;
using EMS.Application.Features.Reimbursements.Handlers;
using EMS.Application.Features.Reimbursements.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Pdf;
using EMS.Infrastructure.Storage;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class ReimbursementTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_reimbursement_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static Mock<IAuthRepository> AuthRepoFor(Guid userId, Guid? employeeId)
        {
            var mock = new Mock<IAuthRepository>();
            mock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = userId, EmployeeId = employeeId, UserName = "u", Email = "u@test.com", PasswordHash = "x" });
            return mock;
        }

        // Reimbursement.Employee is a required (INNER JOIN) navigation — same InMemory-provider
        // gotcha documented in EmployeeTests.cs/TaskManagementTests.cs. Seed a real Employee row.
        private static async Task<Guid> SeedEmployeeAsync(ApplicationDbContext db)
        {
            var designationId = Guid.NewGuid();
            var officeLocationId = Guid.NewGuid();
            db.Designations.Add(new Designation { Id = designationId, Name = "Designation-" + designationId, Code = "DSG-" + designationId.ToString("N")[..8], CreatedAtUtc = DateTime.UtcNow });
            db.OfficeLocations.Add(new OfficeLocation { Id = officeLocationId, Name = "Location-" + officeLocationId, Code = "LOC-" + officeLocationId.ToString("N")[..8], City = "City", Country = "Country", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow });

            var employeeId = Guid.NewGuid();
            db.Employees.Add(new Employee
            {
                Id = employeeId,
                EmployeeCode = "EMP-" + employeeId.ToString("N")[..8],
                FirstName = "Test",
                LastName = "Employee",
                Email = $"{employeeId:N}@test.local",
                JoinDate = DateTime.UtcNow.Date,
                IsActive = true,
                DesignationId = designationId,
                OfficeLocationId = officeLocationId,
                EmploymentStatus = "Active"
            });

            await db.SaveChangesAsync();
            return employeeId;
        }

        private static CreateReimbursementCommand ValidCreateCommand(Guid requestingUserId) => new()
        {
            ExpenseTitle = "Client dinner",
            ExpenseCategory = "Meals",
            ExpenseDate = DateTime.UtcNow.Date.AddDays(-2),
            Amount = 120.50m,
            Currency = "USD",
            RequestingUserId = requestingUserId
        };

        [Fact]
        public async Task CreateReimbursement_PersistsAsDraftForCallersOwnEmployee()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var userId = Guid.NewGuid();
            var authRepo = AuthRepoFor(userId, employeeId).Object;
            var handler = new CreateReimbursementCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);

            var created = await handler.Handle(ValidCreateCommand(userId), CancellationToken.None);

            Assert.StartsWith("REI-", created.ReimbursementNumber);
            Assert.Equal(ReimbursementStatus.Draft, created.Status);
            Assert.Equal(employeeId, created.EmployeeId);
        }

        [Fact]
        public async Task UpdateReimbursement_OwnerCanEditDraft_NonOwnerIsRejected_ApprovedIsReadOnly()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var ownerUserId = Guid.NewGuid();
            var ownerAuthRepo = AuthRepoFor(ownerUserId, employeeId).Object;
            var createHandler = new CreateReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);
            var updateHandler = new UpdateReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<UpdateReimbursementCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(ownerUserId), CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateReimbursementCommand
            {
                Id = created.Id,
                ExpenseTitle = "Client dinner (revised)",
                ExpenseCategory = "Meals",
                ExpenseDate = created.ExpenseDate,
                Amount = 150m,
                Currency = "USD",
                RequestingUserId = ownerUserId
            }, CancellationToken.None);
            Assert.Equal(150m, updated.Amount);

            var otherUserId = Guid.NewGuid();
            var otherAuthRepo = AuthRepoFor(otherUserId, Guid.NewGuid()).Object;
            var updateHandlerAsOther = new UpdateReimbursementCommandHandler(repo, otherAuthRepo, new RecordingAuditLogger(), NullLogger<UpdateReimbursementCommandHandler>.Instance);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                updateHandlerAsOther.Handle(new UpdateReimbursementCommand { Id = created.Id, ExpenseTitle = "Hijack", ExpenseCategory = "Meals", ExpenseDate = created.ExpenseDate, Amount = 1m, Currency = "USD", RequestingUserId = otherUserId }, CancellationToken.None));

            var entity = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            entity!.Status = ReimbursementStatus.Approved;
            await repo.UpdateAsync(entity, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateHandler.Handle(new UpdateReimbursementCommand { Id = created.Id, ExpenseTitle = "Too late", ExpenseCategory = "Meals", ExpenseDate = created.ExpenseDate, Amount = 1m, Currency = "USD", RequestingUserId = ownerUserId }, CancellationToken.None));
        }

        [Fact]
        public async Task SubmitReimbursement_MovesDraftToSubmitted_OnlyFromDraftOrChangesRequested()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var userId = Guid.NewGuid();
            var authRepo = AuthRepoFor(userId, employeeId).Object;
            var createHandler = new CreateReimbursementCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);
            var submitHandler = new SubmitReimbursementCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<SubmitReimbursementCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(userId), CancellationToken.None);
            await submitHandler.Handle(new SubmitReimbursementCommand { Id = created.Id, RequestingUserId = userId }, CancellationToken.None);

            var submitted = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(ReimbursementStatus.Submitted, submitted!.Status);
            Assert.NotNull(submitted.SubmittedAtUtc);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                submitHandler.Handle(new SubmitReimbursementCommand { Id = created.Id, RequestingUserId = userId }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteReimbursement_DraftOnly_OwnerOnly()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var userId = Guid.NewGuid();
            var authRepo = AuthRepoFor(userId, employeeId).Object;
            var createHandler = new CreateReimbursementCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);
            var deleteHandler = new DeleteReimbursementCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<DeleteReimbursementCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(userId), CancellationToken.None);
            await deleteHandler.Handle(new DeleteReimbursementCommand { Id = created.Id, RequestingUserId = userId }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));
            var stillThere = await repo.GetByIdIncludingDeletedAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stillThere);
            Assert.True(stillThere!.IsDeleted);
        }

        [Fact]
        public async Task FullApprovalWorkflow_SubmittedToUnderReviewToApproved_BlocksSelfApproval()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var ownerUserId = Guid.NewGuid();
            var ownerAuthRepo = AuthRepoFor(ownerUserId, employeeId).Object;
            var createHandler = new CreateReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);
            var submitHandler = new SubmitReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<SubmitReimbursementCommandHandler>.Instance);
            var startReviewHandler = new StartReviewReimbursementCommandHandler(repo, new RecordingAuditLogger(), NullLogger<StartReviewReimbursementCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(ownerUserId), CancellationToken.None);
            await submitHandler.Handle(new SubmitReimbursementCommand { Id = created.Id, RequestingUserId = ownerUserId }, CancellationToken.None);
            await startReviewHandler.Handle(new StartReviewReimbursementCommand { Id = created.Id }, CancellationToken.None);

            Assert.Equal(ReimbursementStatus.UnderReview, (await repo.GetByIdAsync(created.Id, CancellationToken.None))!.Status);

            // The claimant themselves, even though they'd need the Admin policy to reach this
            // endpoint at all, must still be blocked at the handler level.
            var selfApproveHandler = new ApproveReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<ApproveReimbursementCommandHandler>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                selfApproveHandler.Handle(new ApproveReimbursementCommand { Id = created.Id, RequestingUserId = ownerUserId }, CancellationToken.None));

            var adminUserId = Guid.NewGuid();
            var adminAuthRepo = AuthRepoFor(adminUserId, Guid.NewGuid()).Object;
            var approveHandler = new ApproveReimbursementCommandHandler(repo, adminAuthRepo, new RecordingAuditLogger(), NullLogger<ApproveReimbursementCommandHandler>.Instance);
            await approveHandler.Handle(new ApproveReimbursementCommand { Id = created.Id, RequestingUserId = adminUserId }, CancellationToken.None);

            var approved = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(ReimbursementStatus.Approved, approved!.Status);
            Assert.Equal(adminUserId, approved.ApprovedBy);
            Assert.NotNull(approved.ApprovedAtUtc);
        }

        [Fact]
        public async Task RejectAndRequestChanges_RequireUnderReview_RecordRemarks()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var ownerUserId = Guid.NewGuid();
            var ownerAuthRepo = AuthRepoFor(ownerUserId, employeeId).Object;
            var adminUserId = Guid.NewGuid();
            var adminAuthRepo = AuthRepoFor(adminUserId, Guid.NewGuid()).Object;

            var createHandler = new CreateReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);
            var submitHandler = new SubmitReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<SubmitReimbursementCommandHandler>.Instance);
            var startReviewHandler = new StartReviewReimbursementCommandHandler(repo, new RecordingAuditLogger(), NullLogger<StartReviewReimbursementCommandHandler>.Instance);
            var rejectHandler = new RejectReimbursementCommandHandler(repo, adminAuthRepo, new RecordingAuditLogger(), NullLogger<RejectReimbursementCommandHandler>.Instance);

            var rejected = await createHandler.Handle(ValidCreateCommand(ownerUserId), CancellationToken.None);
            await submitHandler.Handle(new SubmitReimbursementCommand { Id = rejected.Id, RequestingUserId = ownerUserId }, CancellationToken.None);
            await startReviewHandler.Handle(new StartReviewReimbursementCommand { Id = rejected.Id }, CancellationToken.None);
            await rejectHandler.Handle(new RejectReimbursementCommand { Id = rejected.Id, Remarks = "Missing receipt", RequestingUserId = adminUserId }, CancellationToken.None);

            var rejectedEntity = await repo.GetByIdAsync(rejected.Id, CancellationToken.None);
            Assert.Equal(ReimbursementStatus.Rejected, rejectedEntity!.Status);
            Assert.Equal("Missing receipt", rejectedEntity.ReviewRemarks);

            var changesRequestHandler = new RequestChangesReimbursementCommandHandler(repo, adminAuthRepo, new RecordingAuditLogger(), NullLogger<RequestChangesReimbursementCommandHandler>.Instance);
            var toRevise = await createHandler.Handle(ValidCreateCommand(ownerUserId), CancellationToken.None);
            await submitHandler.Handle(new SubmitReimbursementCommand { Id = toRevise.Id, RequestingUserId = ownerUserId }, CancellationToken.None);
            await startReviewHandler.Handle(new StartReviewReimbursementCommand { Id = toRevise.Id }, CancellationToken.None);
            await changesRequestHandler.Handle(new RequestChangesReimbursementCommand { Id = toRevise.Id, Remarks = "Wrong category", RequestingUserId = adminUserId }, CancellationToken.None);

            var revised = await repo.GetByIdAsync(toRevise.Id, CancellationToken.None);
            Assert.Equal(ReimbursementStatus.ChangesRequested, revised!.Status);

            // ChangesRequested can be edited and resubmitted, closing the loop.
            var updateHandler = new UpdateReimbursementCommandHandler(repo, ownerAuthRepo, new RecordingAuditLogger(), NullLogger<UpdateReimbursementCommandHandler>.Instance);
            await updateHandler.Handle(new UpdateReimbursementCommand { Id = toRevise.Id, ExpenseTitle = "Fixed", ExpenseCategory = "Travel", ExpenseDate = toRevise.ExpenseDate, Amount = 200m, Currency = "USD", RequestingUserId = ownerUserId }, CancellationToken.None);
            await submitHandler.Handle(new SubmitReimbursementCommand { Id = toRevise.Id, RequestingUserId = ownerUserId }, CancellationToken.None);
            Assert.Equal(ReimbursementStatus.Submitted, (await repo.GetByIdAsync(toRevise.Id, CancellationToken.None))!.Status);
        }

        [Fact]
        public async Task GetReimbursementsQuery_NonPrivilegedCaller_IsScopedToOwnReimbursementsRegardlessOfFilter()
        {
            using var db = CreateDb();
            var employeeAId = await SeedEmployeeAsync(db);
            var employeeBId = await SeedEmployeeAsync(db);
            var repo = new ReimbursementRepository(db);
            var userAId = Guid.NewGuid();
            var userBId = Guid.NewGuid();
            var createHandlerA = new CreateReimbursementCommandHandler(repo, AuthRepoFor(userAId, employeeAId).Object, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);
            var createHandlerB = new CreateReimbursementCommandHandler(repo, AuthRepoFor(userBId, employeeBId).Object, new RecordingAuditLogger(), NullLogger<CreateReimbursementCommandHandler>.Instance);

            await createHandlerA.Handle(ValidCreateCommand(userAId), CancellationToken.None);
            await createHandlerB.Handle(ValidCreateCommand(userBId), CancellationToken.None);

            var queryHandler = new GetReimbursementsQueryHandler(repo, AuthRepoFor(userAId, employeeAId).Object);

            var result = await queryHandler.Handle(new GetReimbursementsQuery { EmployeeId = employeeBId, RequestingUserId = userAId, IsPrivileged = false }, CancellationToken.None);

            Assert.Single(result.Data);
            Assert.Equal(employeeAId, result.Data.Single().EmployeeId);
        }

        [Fact]
        public async Task CreateReimbursementCommandValidator_RejectsInvalidInput()
        {
            var validator = new CreateReimbursementCommandValidator();

            var result = await validator.ValidateAsync(new CreateReimbursementCommand
            {
                ExpenseTitle = "",
                ExpenseCategory = "",
                ExpenseDate = DateTime.UtcNow.Date.AddDays(5), // future
                Amount = -10m,
                Currency = "",
                RequestingUserId = Guid.NewGuid()
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ExpenseTitle");
            Assert.Contains(result.Errors, e => e.PropertyName == "ExpenseCategory");
            Assert.Contains(result.Errors, e => e.PropertyName == "ExpenseDate");
            Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
        }

        [Fact]
        public async Task ProcessPayroll_IncludesApprovedReimbursement_AndMarksItPaidExactlyOnce()
        {
            using var db = CreateDb();
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Staff", Code = "STF-" + Guid.NewGuid().ToString("N")[..6], CreatedAtUtc = DateTime.UtcNow };
            await db.Designations.AddAsync(designation);
            var emp = new Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..6], FirstName = "Pay", LastName = "Roll", IsActive = true, JoinDate = DateTime.UtcNow, DesignationId = designation.Id };
            await db.Employees.AddAsync(emp);
            var ss = new SalaryStructure { Id = Guid.NewGuid(), EmployeeId = emp.Id, BasicSalary = 1000m, EffectiveFrom = DateTime.UtcNow.AddMonths(-1) };
            await db.SalaryStructures.AddAsync(ss);
            await db.SaveChangesAsync();

            var reimbursementRepo = new ReimbursementRepository(db);
            var reimbursementId = Guid.NewGuid();
            await db.Reimbursements.AddAsync(new Reimbursement
            {
                Id = reimbursementId,
                ReimbursementNumber = "REI-TESTPAY01",
                EmployeeId = emp.Id,
                ExpenseTitle = "Taxi",
                ExpenseCategory = "Travel",
                ExpenseDate = DateTime.UtcNow.Date.AddDays(-3),
                Amount = 75m,
                Currency = "USD",
                Status = ReimbursementStatus.Approved,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var payrollRepo = new PayrollRepository(db);
            var pdf = new PdfSharpDocumentService();
            var tempBase = Path.Combine(Path.GetTempPath(), "ems-reimbursement-payroll-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var storage = new LocalFileStorageService(tempBase);
            var handler = new ProcessPayrollCommandHandler(payrollRepo, reimbursementRepo, pdf, storage, new RecordingAuditLogger(), NullLogger<ProcessPayrollCommandHandler>.Instance);

            var runId = await handler.Handle(new ProcessPayrollCommand { PeriodStart = DateTime.UtcNow.AddDays(-7), PeriodEnd = DateTime.UtcNow, ProcessedBy = Guid.NewGuid() }, CancellationToken.None);

            var payslip = db.Payslips.Single(p => p.EmployeeId == emp.Id);
            Assert.Equal(75m, payslip.TotalReimbursements);
            Assert.Equal(1000m + 75m, payslip.NetPay); // no allowances/deductions on this structure

            var processedReimbursement = await reimbursementRepo.GetByIdAsync(reimbursementId, CancellationToken.None);
            Assert.Equal(ReimbursementStatus.Paid, processedReimbursement!.Status);
            Assert.True(processedReimbursement.PayrollProcessed);
            Assert.Equal(runId, processedReimbursement.PayrollRunId);

            // A second run must not pick the now-Paid reimbursement up again.
            var secondRunId = await handler.Handle(new ProcessPayrollCommand { PeriodStart = DateTime.UtcNow, PeriodEnd = DateTime.UtcNow.AddDays(7), ProcessedBy = Guid.NewGuid() }, CancellationToken.None);
            var secondPayslip = db.Payslips.Single(p => p.PayrollRunId == secondRunId && p.EmployeeId == emp.Id);
            Assert.Equal(0m, secondPayslip.TotalReimbursements);

            try { Directory.Delete(tempBase, true); } catch { }
        }
    }
}

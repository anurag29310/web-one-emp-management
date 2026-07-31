using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Features.Payroll.Handlers;
using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Features.Payroll.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Pdf;
using EMS.Infrastructure.Storage;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    public class PayrollTests
    {
        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static readonly Guid TestCompanyId = Guid.NewGuid();

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public Guid? CompanyId => TestCompanyId;
            public string? IpAddress => null;
            public string? UserAgent => null;
        }

        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_payroll_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<User> SeedUserAsync(ApplicationDbContext db, Guid? employeeId)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user_" + Guid.NewGuid().ToString("N")[..8],
                Email = Guid.NewGuid() + "@test.local",
                PasswordHash = "hash",
                EmployeeId = employeeId
            };
            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
            return user;
        }

        private static async Task<Payslip> SeedPayslipWithDocumentAsync(ApplicationDbContext db, string storageBasePath, Guid employeeId)
        {
            var payslip = new Payslip
            {
                Id = Guid.NewGuid(),
                PayrollRunId = Guid.NewGuid(),
                EmployeeId = employeeId,
                Basic = 1000m,
                TotalAllowances = 100m,
                TotalDeductions = 50m,
                GrossPay = 1100m,
                NetPay = 1050m,
                GeneratedAtUtc = DateTime.UtcNow,
                BlobContainer = "payslips",
                BlobPath = $"{Guid.NewGuid()}/payslip.pdf"
            };
            await db.Payslips.AddAsync(payslip);
            await db.SaveChangesAsync();

            var storage = new LocalFileStorageService(storageBasePath);
            await storage.SaveFileAsync(payslip.BlobContainer, payslip.BlobPath, new byte[] { 1, 2, 3 }, "application/pdf");

            return payslip;
        }

        [Fact]
        public async Task ProcessPayroll_CreatesPayslipsAndPdf()
        {
            var dbName = "ems_payroll_test_" + Guid.NewGuid();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);

            using var db = new ApplicationDbContext(options);
            // create employee
            var designation = new EMS.Domain.Entities.Designation { Id = Guid.NewGuid(), Name = "Staff", Code = "STF", CreatedAtUtc = DateTime.UtcNow };
            await db.Designations.AddAsync(designation);
            var emp = new EMS.Domain.Entities.Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-1", FirstName = "Test", LastName = "User", IsActive = true, JoinDate = DateTime.UtcNow, DesignationId = designation.Id };
            await db.Employees.AddAsync(emp);

            // salary structure
            var ss = new EMS.Domain.Entities.SalaryStructure { Id = Guid.NewGuid(), EmployeeId = emp.Id, BasicSalary = 1000m, EffectiveFrom = DateTime.UtcNow.AddMonths(-1) };
            await db.SalaryStructures.AddAsync(ss);
            var al = new EMS.Domain.Entities.Allowance { Id = Guid.NewGuid(), SalaryStructureId = ss.Id, Name = "House", Amount = 100m };
            var ded = new EMS.Domain.Entities.Deduction { Id = Guid.NewGuid(), SalaryStructureId = ss.Id, Name = "Tax", Amount = 50m };
            await db.Allowances.AddAsync(al);
            await db.Deductions.AddAsync(ded);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var reimbursementRepo = new ReimbursementRepository(db);
            var attendanceRepo = new AttendanceRepository(db);
            var pdf = new PdfSharpDocumentService();
            var storage = new LocalFileStorageService(tempBase);
            var logger = new NullLogger<EMS.Application.Features.Payroll.Handlers.ProcessPayrollCommandHandler>();
            var config = new ConfigurationBuilder().Build();

            var handler = new EMS.Application.Features.Payroll.Handlers.ProcessPayrollCommandHandler(repo, reimbursementRepo, attendanceRepo, pdf, storage, new RecordingAuditLogger(), new FakeCurrentUserService(), config, logger);

            var cmd = new ProcessPayrollCommand { PeriodStart = DateTime.UtcNow.AddDays(-7), PeriodEnd = DateTime.UtcNow, ProcessedBy = Guid.NewGuid() };
            var runId = await handler.Handle(cmd, CancellationToken.None);

            var payslip = db.Payslips.FirstOrDefault();
            Assert.NotNull(payslip);
            Assert.Equal(1000m, payslip.Basic);

            var expectedPath = Path.Combine(tempBase, "Storage", "payslips", runId.ToString(), payslip.Id + ".pdf");
            Assert.True(File.Exists(expectedPath));

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task DownloadPayslip_OwnPayslip_Succeeds()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var payslip = await SeedPayslipWithDocumentAsync(db, tempBase, employeeId);

            var repo = new PayrollRepository(db);
            var authRepo = new AuthRepository(db);
            var storage = new LocalFileStorageService(tempBase);
            var handler = new DownloadPayslipQueryHandler(repo, authRepo, storage, NullLogger<DownloadPayslipQueryHandler>.Instance);

            var result = await handler.Handle(new DownloadPayslipQuery
            {
                PayslipId = payslip.Id,
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task DownloadPayslip_AnotherEmployeesPayslip_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var user = await SeedUserAsync(db, Guid.NewGuid());

            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var payslip = await SeedPayslipWithDocumentAsync(db, tempBase, Guid.NewGuid()); // belongs to someone else

            var repo = new PayrollRepository(db);
            var authRepo = new AuthRepository(db);
            var storage = new LocalFileStorageService(tempBase);
            var handler = new DownloadPayslipQueryHandler(repo, authRepo, storage, NullLogger<DownloadPayslipQueryHandler>.Instance);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new DownloadPayslipQuery
            {
                PayslipId = payslip.Id,
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None));

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task DownloadPayslip_Privileged_CanDownloadAnyEmployeesPayslip()
        {
            using var db = CreateDb();
            var hrUser = await SeedUserAsync(db, null);

            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var payslip = await SeedPayslipWithDocumentAsync(db, tempBase, Guid.NewGuid());

            var repo = new PayrollRepository(db);
            var authRepo = new AuthRepository(db);
            var storage = new LocalFileStorageService(tempBase);
            var handler = new DownloadPayslipQueryHandler(repo, authRepo, storage, NullLogger<DownloadPayslipQueryHandler>.Instance);

            var result = await handler.Handle(new DownloadPayslipQuery
            {
                PayslipId = payslip.Id,
                RequestingUserId = hrUser.Id,
                IsPrivileged = true
            }, CancellationToken.None);

            Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task DownloadPayslip_UnknownId_ThrowsNotFound()
        {
            using var db = CreateDb();
            var user = await SeedUserAsync(db, Guid.NewGuid());

            var repo = new PayrollRepository(db);
            var authRepo = new AuthRepository(db);
            var storage = new LocalFileStorageService(Path.GetTempPath());
            var handler = new DownloadPayslipQueryHandler(repo, authRepo, storage, NullLogger<DownloadPayslipQueryHandler>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new DownloadPayslipQuery
            {
                PayslipId = Guid.NewGuid(),
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None));

            Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetPayslipsForEmployee_NonPrivileged_IsScopedToOwnRecord()
        {
            using var db = CreateDb();
            var employeeId = Guid.NewGuid();
            var user = await SeedUserAsync(db, employeeId);

            await db.Payslips.AddAsync(new Payslip { Id = Guid.NewGuid(), PayrollRunId = Guid.NewGuid(), EmployeeId = employeeId, Basic = 1000m, NetPay = 950m, GeneratedAtUtc = DateTime.UtcNow });
            await db.Payslips.AddAsync(new Payslip { Id = Guid.NewGuid(), PayrollRunId = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), Basic = 2000m, NetPay = 1900m, GeneratedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new GetPayslipsForEmployeeQueryHandler(repo, authRepo);

            var result = await handler.Handle(new GetPayslipsForEmployeeQuery
            {
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None);

            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(employeeId, list[0].EmployeeId);
        }

        [Fact]
        public async Task GetPayslipsForEmployee_NonPrivileged_RequestingAnotherEmployeeId_ThrowsUnauthorized()
        {
            using var db = CreateDb();
            var user = await SeedUserAsync(db, Guid.NewGuid());

            var repo = new PayrollRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new GetPayslipsForEmployeeQueryHandler(repo, authRepo);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new GetPayslipsForEmployeeQuery
            {
                EmployeeId = Guid.NewGuid(), // not the requester's own employee id
                RequestingUserId = user.Id,
                IsPrivileged = false
            }, CancellationToken.None));
        }

        [Fact]
        public async Task ApprovePayrollRun_WhenCompleted_ApprovesSuccessfully()
        {
            using var db = CreateDb();
            var run = new PayrollRun { Id = Guid.NewGuid(), PeriodStart = DateTime.UtcNow.AddDays(-30), PeriodEnd = DateTime.UtcNow, ProcessedAtUtc = DateTime.UtcNow, ProcessedBy = Guid.NewGuid(), Status = "Completed" };
            await db.PayrollRuns.AddAsync(run);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var handler = new ApprovePayrollRunCommandHandler(repo, new RecordingAuditLogger(), NullLogger<ApprovePayrollRunCommandHandler>.Instance);

            await handler.Handle(new ApprovePayrollRunCommand { PayrollRunId = run.Id, ApprovedBy = Guid.NewGuid() }, CancellationToken.None);

            var updated = await db.PayrollRuns.FindAsync(run.Id);
            Assert.Equal("Approved", updated!.Status);
        }

        [Fact]
        public async Task ApprovePayrollRun_WhenNotCompleted_Throws()
        {
            using var db = CreateDb();
            var run = new PayrollRun { Id = Guid.NewGuid(), PeriodStart = DateTime.UtcNow.AddDays(-30), PeriodEnd = DateTime.UtcNow, ProcessedAtUtc = DateTime.UtcNow, ProcessedBy = Guid.NewGuid(), Status = "Processing" };
            await db.PayrollRuns.AddAsync(run);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var handler = new ApprovePayrollRunCommandHandler(repo, new RecordingAuditLogger(), NullLogger<ApprovePayrollRunCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new ApprovePayrollRunCommand { PayrollRunId = run.Id, ApprovedBy = Guid.NewGuid() }, CancellationToken.None));
        }

        [Fact]
        public async Task ApprovePayrollRun_WhenAlreadyApproved_Throws()
        {
            using var db = CreateDb();
            var run = new PayrollRun { Id = Guid.NewGuid(), PeriodStart = DateTime.UtcNow.AddDays(-30), PeriodEnd = DateTime.UtcNow, ProcessedAtUtc = DateTime.UtcNow, ProcessedBy = Guid.NewGuid(), Status = "Approved" };
            await db.PayrollRuns.AddAsync(run);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var handler = new ApprovePayrollRunCommandHandler(repo, new RecordingAuditLogger(), NullLogger<ApprovePayrollRunCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new ApprovePayrollRunCommand { PayrollRunId = run.Id, ApprovedBy = Guid.NewGuid() }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteSalaryStructure_UnknownId_Throws()
        {
            using var db = CreateDb();
            var repo = new PayrollRepository(db);
            var handler = new EMS.Application.Features.Payroll.Handlers.DeleteSalaryStructureCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<EMS.Application.Features.Payroll.Handlers.DeleteSalaryStructureCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new EMS.Application.Features.Payroll.Commands.DeleteSalaryStructureCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }

        [Fact]
        public async Task SalaryStructure_SoftDeleteThenRestore_ExcludesThenReincludesFromQueries()
        {
            using var db = CreateDb();
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Staff", Code = "STF-" + Guid.NewGuid().ToString("N")[..6], CreatedAtUtc = DateTime.UtcNow };
            await db.Designations.AddAsync(designation);
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..6], FirstName = "Test", LastName = "User", IsActive = true, JoinDate = DateTime.UtcNow, DesignationId = designation.Id };
            await db.Employees.AddAsync(employee);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var createHandler = new CreateSalaryStructureCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger());
            var deleteHandler = new EMS.Application.Features.Payroll.Handlers.DeleteSalaryStructureCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<EMS.Application.Features.Payroll.Handlers.DeleteSalaryStructureCommandHandler>.Instance);
            var restoreHandler = new EMS.Application.Features.Payroll.Handlers.RestoreSalaryStructureCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<EMS.Application.Features.Payroll.Handlers.RestoreSalaryStructureCommandHandler>.Instance);

            var structureId = await createHandler.Handle(new CreateSalaryStructureCommand
            {
                EmployeeId = employee.Id,
                BasicSalary = 1000m,
                EffectiveFrom = DateTime.UtcNow.AddMonths(-1)
            }, CancellationToken.None);

            var created = await db.SalaryStructures.FindAsync(structureId);
            Assert.NotEqual(default, created!.CreatedAtUtc);
            Assert.False(created.IsDeleted);

            await deleteHandler.Handle(new EMS.Application.Features.Payroll.Commands.DeleteSalaryStructureCommand { Id = structureId }, CancellationToken.None);

            // Soft-deleted: gone from every normal read path...
            Assert.Null(await repo.GetSalaryStructureByIdAsync(structureId));
            Assert.DoesNotContain(await repo.GetSalaryStructuresAsync(), s => s.Id == structureId);
            Assert.Null(await repo.GetEffectiveSalaryStructureAsync(employee.Id, DateTime.UtcNow));

            // ...but the row itself still exists, marked deleted with who/when.
            var deletedRow = await repo.GetSalaryStructureByIdIncludingDeletedAsync(structureId);
            Assert.NotNull(deletedRow);
            Assert.True(deletedRow!.IsDeleted);
            Assert.NotNull(deletedRow.DeletedAtUtc);
            Assert.NotNull(deletedRow.DeletedBy);

            await restoreHandler.Handle(new EMS.Application.Features.Payroll.Commands.RestoreSalaryStructureCommand { Id = structureId }, CancellationToken.None);

            var restored = await repo.GetSalaryStructureByIdAsync(structureId);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
            Assert.Null(restored.DeletedAtUtc);
            Assert.Null(restored.DeletedBy);
            Assert.NotNull(restored.UpdatedAtUtc);
            Assert.NotNull(restored.UpdatedBy);
            Assert.NotNull(await repo.GetEffectiveSalaryStructureAsync(employee.Id, DateTime.UtcNow));
        }

        [Fact]
        public async Task ProcessPayroll_IgnoresSoftDeletedSalaryStructure()
        {
            using var db = CreateDb();
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Staff", Code = "STF-" + Guid.NewGuid().ToString("N")[..6], CreatedAtUtc = DateTime.UtcNow };
            await db.Designations.AddAsync(designation);
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..6], FirstName = "Test", LastName = "User", IsActive = true, JoinDate = DateTime.UtcNow, DesignationId = designation.Id };
            await db.Employees.AddAsync(employee);
            await db.SalaryStructures.AddAsync(new SalaryStructure { Id = Guid.NewGuid(), EmployeeId = employee.Id, BasicSalary = 1000m, EffectiveFrom = DateTime.UtcNow.AddMonths(-1), IsDeleted = true, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var payrollRepo = new PayrollRepository(db);
            var reimbursementRepo = new ReimbursementRepository(db);
            var attendanceRepo = new AttendanceRepository(db);
            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-softdelete-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var handler = new ProcessPayrollCommandHandler(payrollRepo, reimbursementRepo, attendanceRepo, new PdfSharpDocumentService(), new LocalFileStorageService(tempBase), new RecordingAuditLogger(), new FakeCurrentUserService(), new ConfigurationBuilder().Build(), NullLogger<ProcessPayrollCommandHandler>.Instance);

            await handler.Handle(new ProcessPayrollCommand { PeriodStart = DateTime.UtcNow.AddDays(-7), PeriodEnd = DateTime.UtcNow, ProcessedBy = Guid.NewGuid() }, CancellationToken.None);

            Assert.False(await db.Payslips.AnyAsync(p => p.EmployeeId == employee.Id));

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task ApprovePayrollRun_StampsUpdatedAtAndUpdatedBy()
        {
            using var db = CreateDb();
            var run = new PayrollRun { Id = Guid.NewGuid(), PeriodStart = DateTime.UtcNow.AddDays(-30), PeriodEnd = DateTime.UtcNow, ProcessedAtUtc = DateTime.UtcNow, ProcessedBy = Guid.NewGuid(), Status = "Completed" };
            await db.PayrollRuns.AddAsync(run);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var handler = new ApprovePayrollRunCommandHandler(repo, new RecordingAuditLogger(), NullLogger<ApprovePayrollRunCommandHandler>.Instance);
            var approverId = Guid.NewGuid();

            await handler.Handle(new ApprovePayrollRunCommand { PayrollRunId = run.Id, ApprovedBy = approverId }, CancellationToken.None);

            var updated = await db.PayrollRuns.FindAsync(run.Id);
            Assert.Equal("Approved", updated!.Status);
            Assert.NotNull(updated.UpdatedAtUtc);
            Assert.Equal(approverId, updated.UpdatedBy);
        }

        [Theory]
        [InlineData(-7, -1, true)]   // valid past period
        [InlineData(-1, -7, false)]  // start after end
        [InlineData(-7, 7, false)]   // end in the future
        public void ProcessPayrollCommandValidator_EnforcesPeriodRules(int startOffsetDays, int endOffsetDays, bool expectedValid)
        {
            var validator = new ProcessPayrollCommandValidator();
            var cmd = new ProcessPayrollCommand
            {
                PeriodStart = DateTime.UtcNow.AddDays(startOffsetDays),
                PeriodEnd = DateTime.UtcNow.AddDays(endOffsetDays),
                ProcessedBy = Guid.NewGuid()
            };

            var result = validator.Validate(cmd);
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Fact]
        public void GetPayslipsForEmployeeQueryValidator_PrivilegedWithoutEmployeeId_IsInvalid()
        {
            var validator = new GetPayslipsForEmployeeQueryValidator();
            var result = validator.Validate(new GetPayslipsForEmployeeQuery { IsPrivileged = true, RequestingUserId = Guid.NewGuid() });
            Assert.False(result.IsValid);
        }

        [Fact]
        public void GetPayslipsForEmployeeQueryValidator_NonPrivilegedWithoutEmployeeId_IsValid()
        {
            var validator = new GetPayslipsForEmployeeQueryValidator();
            var result = validator.Validate(new GetPayslipsForEmployeeQuery { IsPrivileged = false, RequestingUserId = Guid.NewGuid() });
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CreateSalaryStructureCommandValidator_RejectsNonexistentEmployee()
        {
            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

            var validator = new CreateSalaryStructureCommandValidator(employeeRepo.Object);

            var result = await validator.ValidateAsync(new CreateSalaryStructureCommand
            {
                EmployeeId = Guid.NewGuid(),
                BasicSalary = 1000m,
                EffectiveFrom = DateTime.UtcNow
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EmployeeId");
        }

        // Regression test for a bug where SalaryStructureConfiguration bound Allowances/Deductions
        // via the untyped HasMany<Allowance>() instead of HasMany(s => s.Allowances), causing EF Core
        // to create a second, undeclared relationship with its own always-null shadow FK column
        // (SalaryStructureId1) for the exact navigation .Include(s => s.Allowances) queries through.
        // Against the real (relational) provider this made GetEffectiveSalaryStructureAsync's
        // .Include() join on the wrong, always-empty column — Payroll would silently compute $0
        // allowances/deductions for every employee. It also meant "replace children" on Update only
        // cleared the shadow FK (an optional relationship, so EF nulls rather than deletes), leaving
        // every prior Allowance/Deduction row permanently orphaned in the table. Both are exercised
        // below; see SalaryStructureConfiguration.cs and the FixPayrollRelationshipsAndForeignKeys
        // migration for the fix.
        [Fact]
        public async Task SalaryStructure_AllowancesAndDeductions_RoundTripThroughRealNavigation()
        {
            using var db = CreateDb();
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Staff", Code = "STF-" + Guid.NewGuid().ToString("N")[..6], CreatedAtUtc = DateTime.UtcNow };
            await db.Designations.AddAsync(designation);
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..6], FirstName = "Test", LastName = "User", IsActive = true, JoinDate = DateTime.UtcNow, DesignationId = designation.Id };
            await db.Employees.AddAsync(employee);
            await db.SaveChangesAsync();

            var repo = new PayrollRepository(db);
            var createHandler = new CreateSalaryStructureCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger());

            var structureId = await createHandler.Handle(new CreateSalaryStructureCommand
            {
                EmployeeId = employee.Id,
                BasicSalary = 1000m,
                EffectiveFrom = DateTime.UtcNow.AddMonths(-1),
                Allowances = new() { new() { Name = "House", Amount = 100m } },
                Deductions = new() { new() { Name = "Tax", Amount = 50m } }
            }, CancellationToken.None);

            var effective = await repo.GetEffectiveSalaryStructureAsync(employee.Id, DateTime.UtcNow);
            Assert.NotNull(effective);
            Assert.Single(effective!.Allowances!);
            Assert.Equal("House", effective.Allowances![0].Name);
            Assert.Single(effective.Deductions!);
            Assert.Equal("Tax", effective.Deductions![0].Name);

            var updateHandler = new UpdateSalaryStructureCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger());
            await updateHandler.Handle(new UpdateSalaryStructureCommand
            {
                Id = structureId,
                BasicSalary = 1200m,
                EffectiveFrom = DateTime.UtcNow.AddMonths(-1),
                Allowances = new() { new() { Name = "Transport", Amount = 75m } },
                Deductions = new()
            }, CancellationToken.None);

            // The old "House" allowance and "Tax" deduction must be gone entirely, not merely
            // detached from the salary structure — a real DELETE, not an orphaned row.
            Assert.Equal(1, await db.Allowances.CountAsync());
            Assert.Equal("Transport", (await db.Allowances.SingleAsync()).Name);
            Assert.Equal(0, await db.Deductions.CountAsync());
        }

        // ─── Bonus & Overtime (requirements.md Payroll Management) ────────────────────

        private static async Task<(Employee Employee, Shift Shift)> SeedEmployeeWithShiftAsync(ApplicationDbContext db, DateTime effectiveFrom)
        {
            var designation = new Designation { Id = Guid.NewGuid(), Name = "Staff", Code = "STF-" + Guid.NewGuid().ToString("N")[..6], CreatedAtUtc = DateTime.UtcNow };
            await db.Designations.AddAsync(designation);
            var employee = new Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..6], FirstName = "Test", LastName = "User", IsActive = true, JoinDate = DateTime.UtcNow, DesignationId = designation.Id };
            await db.Employees.AddAsync(employee);

            var shift = new Shift { Id = Guid.NewGuid(), Name = "Day Shift", StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), GraceMinutes = 10, CreatedAtUtc = DateTime.UtcNow };
            await db.Shifts.AddAsync(shift);
            await db.EmployeeShifts.AddAsync(new EmployeeShift { Id = Guid.NewGuid(), EmployeeId = employee.Id, ShiftId = shift.Id, EffectiveFrom = effectiveFrom, CreatedAtUtc = DateTime.UtcNow });

            await db.SaveChangesAsync();
            return (employee, shift);
        }

        [Fact]
        public async Task ProcessPayroll_AutoCalculatesOvertimeFromAttendanceVsShift()
        {
            using var db = CreateDb();
            var periodStart = DateTime.UtcNow.Date.AddDays(-7);
            var periodEnd = DateTime.UtcNow.Date;
            var (employee, shift) = await SeedEmployeeWithShiftAsync(db, periodStart.AddDays(-30));

            // 2080 / 208 standard monthly hours = $10/hr. Shift is 9:00-17:00 (480 standard minutes);
            // the employee worked 600 minutes (10 hours) that day, so 120 minutes = 2 hours of overtime.
            await db.SalaryStructures.AddAsync(new SalaryStructure { Id = Guid.NewGuid(), EmployeeId = employee.Id, BasicSalary = 2080m, EffectiveFrom = periodStart.AddDays(-30) });
            await db.AttendanceRecords.AddAsync(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ShiftId = shift.Id,
                AttendanceDate = periodStart.AddDays(2),
                CheckInAtUtc = periodStart.AddDays(2).AddHours(9),
                CheckOutAtUtc = periodStart.AddDays(2).AddHours(19),
                TotalWorkMinutes = 600,
                Status = EMS.Domain.Enums.AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var payrollRepo = new PayrollRepository(db);
            var reimbursementRepo = new ReimbursementRepository(db);
            var attendanceRepo = new AttendanceRepository(db);
            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-overtime-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var handler = new ProcessPayrollCommandHandler(payrollRepo, reimbursementRepo, attendanceRepo, new PdfSharpDocumentService(), new LocalFileStorageService(tempBase), new RecordingAuditLogger(), new FakeCurrentUserService(), new ConfigurationBuilder().Build(), NullLogger<ProcessPayrollCommandHandler>.Instance);

            await handler.Handle(new ProcessPayrollCommand { PeriodStart = periodStart, PeriodEnd = periodEnd, ProcessedBy = Guid.NewGuid() }, CancellationToken.None);

            var payslip = await db.Payslips.SingleAsync(p => p.EmployeeId == employee.Id);
            Assert.Equal(2m, payslip.OvertimeHours);
            Assert.Equal(30m, payslip.TotalOvertime); // 2 hours * $10/hr * 1.5x
            Assert.Equal(0m, payslip.TotalBonus); // no Adjustment supplied
            Assert.Equal(2080m + 30m, payslip.GrossPay);
            Assert.Equal(2080m + 30m, payslip.NetPay);

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task ProcessPayroll_AdjustmentOvertimeOverride_WinsOverAutoCalculation()
        {
            using var db = CreateDb();
            var periodStart = DateTime.UtcNow.Date.AddDays(-7);
            var periodEnd = DateTime.UtcNow.Date;
            var (employee, shift) = await SeedEmployeeWithShiftAsync(db, periodStart.AddDays(-30));

            await db.SalaryStructures.AddAsync(new SalaryStructure { Id = Guid.NewGuid(), EmployeeId = employee.Id, BasicSalary = 2080m, EffectiveFrom = periodStart.AddDays(-30) });
            await db.AttendanceRecords.AddAsync(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ShiftId = shift.Id,
                AttendanceDate = periodStart.AddDays(2),
                TotalWorkMinutes = 600, // would auto-calculate to $30, same as the test above
                Status = EMS.Domain.Enums.AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var payrollRepo = new PayrollRepository(db);
            var reimbursementRepo = new ReimbursementRepository(db);
            var attendanceRepo = new AttendanceRepository(db);
            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-overtime-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var handler = new ProcessPayrollCommandHandler(payrollRepo, reimbursementRepo, attendanceRepo, new PdfSharpDocumentService(), new LocalFileStorageService(tempBase), new RecordingAuditLogger(), new FakeCurrentUserService(), new ConfigurationBuilder().Build(), NullLogger<ProcessPayrollCommandHandler>.Instance);

            await handler.Handle(new ProcessPayrollCommand
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ProcessedBy = Guid.NewGuid(),
                Adjustments = new() { new() { EmployeeId = employee.Id, OvertimeAmount = 999m, BonusAmount = 200m } }
            }, CancellationToken.None);

            var payslip = await db.Payslips.SingleAsync(p => p.EmployeeId == employee.Id);
            Assert.Equal(999m, payslip.TotalOvertime); // override wins over the $30 auto-calculation
            Assert.Equal(0m, payslip.OvertimeHours); // not derived from hours when manually overridden
            Assert.Equal(200m, payslip.TotalBonus); // manual-only, applied because an Adjustment was supplied
            Assert.Equal(2080m + 999m + 200m, payslip.GrossPay);

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task ProcessPayroll_EmployeeWithoutAdjustmentOrOvertimeAttendance_GetsZeroBonusAndOvertime()
        {
            using var db = CreateDb();
            var periodStart = DateTime.UtcNow.Date.AddDays(-7);
            var periodEnd = DateTime.UtcNow.Date;
            var (employee, _) = await SeedEmployeeWithShiftAsync(db, periodStart.AddDays(-30));
            await db.SalaryStructures.AddAsync(new SalaryStructure { Id = Guid.NewGuid(), EmployeeId = employee.Id, BasicSalary = 2080m, EffectiveFrom = periodStart.AddDays(-30) });
            await db.SaveChangesAsync();

            var payrollRepo = new PayrollRepository(db);
            var reimbursementRepo = new ReimbursementRepository(db);
            var attendanceRepo = new AttendanceRepository(db);
            var tempBase = Path.Combine(Path.GetTempPath(), "ems-payroll-overtime-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempBase);
            var handler = new ProcessPayrollCommandHandler(payrollRepo, reimbursementRepo, attendanceRepo, new PdfSharpDocumentService(), new LocalFileStorageService(tempBase), new RecordingAuditLogger(), new FakeCurrentUserService(), new ConfigurationBuilder().Build(), NullLogger<ProcessPayrollCommandHandler>.Instance);

            await handler.Handle(new ProcessPayrollCommand { PeriodStart = periodStart, PeriodEnd = periodEnd, ProcessedBy = Guid.NewGuid() }, CancellationToken.None);

            var payslip = await db.Payslips.SingleAsync(p => p.EmployeeId == employee.Id);
            Assert.Equal(0m, payslip.TotalBonus);
            Assert.Equal(0m, payslip.TotalOvertime);
            Assert.Equal(2080m, payslip.GrossPay);

            try { Directory.Delete(tempBase, true); } catch { }
        }

        [Fact]
        public async Task DryRunPayroll_MirrorsProcessPayroll_ForAutoCalculatedOvertime()
        {
            using var db = CreateDb();
            var periodStart = DateTime.UtcNow.Date.AddDays(-7);
            var periodEnd = DateTime.UtcNow.Date;
            var (employee, shift) = await SeedEmployeeWithShiftAsync(db, periodStart.AddDays(-30));
            await db.SalaryStructures.AddAsync(new SalaryStructure { Id = Guid.NewGuid(), EmployeeId = employee.Id, BasicSalary = 2080m, EffectiveFrom = periodStart.AddDays(-30) });
            await db.AttendanceRecords.AddAsync(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ShiftId = shift.Id,
                AttendanceDate = periodStart.AddDays(2),
                TotalWorkMinutes = 600,
                Status = EMS.Domain.Enums.AttendanceStatus.Present,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var payrollRepo = new PayrollRepository(db);
            var reimbursementRepo = new ReimbursementRepository(db);
            var attendanceRepo = new AttendanceRepository(db);
            var handler = new DryRunPayrollQueryHandler(payrollRepo, reimbursementRepo, attendanceRepo, new FakeCurrentUserService(), new ConfigurationBuilder().Build());

            var previews = await handler.Handle(new DryRunPayrollQuery { PeriodStart = periodStart, PeriodEnd = periodEnd }, CancellationToken.None);
            var preview = previews.Single(p => p.EmployeeId == employee.Id);

            Assert.Equal(2m, preview.OvertimeHours);
            Assert.Equal(30m, preview.TotalOvertime);
            Assert.Equal(2080m + 30m, preview.GrossPay);
        }

        [Theory]
        [InlineData(9, 0, 17, 0, 480)]   // day shift, 8 hours
        [InlineData(22, 0, 6, 0, 480)]   // night shift wrapping past midnight, 8 hours
        public void OvertimeCalculator_StandardDailyMinutes_HandlesNightShifts(int startH, int startM, int endH, int endM, int expectedMinutes)
        {
            var shift = new Shift { StartTime = new TimeSpan(startH, startM, 0), EndTime = new TimeSpan(endH, endM, 0) };
            Assert.Equal(expectedMinutes, EMS.Application.Features.Payroll.OvertimeCalculator.StandardDailyMinutes(shift, 480));
        }

        [Fact]
        public void OvertimeCalculator_NoShift_FallsBackToDefaultDailyMinutes()
        {
            Assert.Equal(480, EMS.Application.Features.Payroll.OvertimeCalculator.StandardDailyMinutes(null, 480));
        }

        [Theory]
        [InlineData(600, 480, 120)]
        [InlineData(400, 480, 0)]  // under standard — never negative overtime
        [InlineData(null, 480, 0)] // never checked out
        public void OvertimeCalculator_OvertimeMinutesForDay_NeverGoesNegative(int? totalWorkMinutes, int standardMinutes, int expected)
        {
            Assert.Equal(expected, EMS.Application.Features.Payroll.OvertimeCalculator.OvertimeMinutesForDay(totalWorkMinutes, standardMinutes));
        }
    }
}

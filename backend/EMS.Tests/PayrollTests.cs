using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Features.Payroll.Handlers;
using EMS.Application.Features.Payroll.Queries;
using EMS.Application.Features.Payroll.Validators;
using EMS.Domain.Entities;
using EMS.Infrastructure.Pdf;
using EMS.Infrastructure.Storage;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
            var emp = new EMS.Domain.Entities.Employee { Id = Guid.NewGuid(), EmployeeCode = "EMP-1", FirstName = "Test", LastName = "User", IsActive = true, JoinDate = DateTime.UtcNow };
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
            var pdf = new SimplePdfService();
            var storage = new LocalFileStorageService(tempBase);
            var logger = new NullLogger<EMS.Application.Features.Payroll.Handlers.ProcessPayrollCommandHandler>();

            var handler = new EMS.Application.Features.Payroll.Handlers.ProcessPayrollCommandHandler(repo, pdf, storage, logger);

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
            var handler = new ApprovePayrollRunCommandHandler(repo, NullLogger<ApprovePayrollRunCommandHandler>.Instance);

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
            var handler = new ApprovePayrollRunCommandHandler(repo, NullLogger<ApprovePayrollRunCommandHandler>.Instance);

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
            var handler = new ApprovePayrollRunCommandHandler(repo, NullLogger<ApprovePayrollRunCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new ApprovePayrollRunCommand { PayrollRunId = run.Id, ApprovedBy = Guid.NewGuid() }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteSalaryStructure_UnknownId_Throws()
        {
            using var db = CreateDb();
            var repo = new PayrollRepository(db);
            var handler = new EMS.Application.Features.Payroll.Handlers.DeleteSalaryStructureCommandHandler(repo);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new EMS.Application.Features.Payroll.Commands.DeleteSalaryStructureCommand { Id = Guid.NewGuid() }, CancellationToken.None));
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
    }
}

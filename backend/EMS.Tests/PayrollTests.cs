using EMS.Application.Features.Payroll.Commands;
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

            var expectedPath = Path.Combine(tempBase, "payslips", runId.ToString(), payslip.Id + ".pdf");
            Assert.True(File.Exists(expectedPath));

            try { Directory.Delete(tempBase, true); } catch { }
        }
    }
}

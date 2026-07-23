using EMS.Application.Features.Reports.Handlers;
using EMS.Application.Features.Reports.Queries;
using EMS.Application.Features.Reports.Validators;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Export;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class ReportsTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_reports_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetEmployeeReport_CountsActiveAndInactive()
        {
            using var db = CreateDb();
            await db.Employees.AddRangeAsync(
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "One", JoinDate = DateTime.UtcNow, IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "B", LastName = "Two", JoinDate = DateTime.UtcNow, IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E3", FirstName = "C", LastName = "Three", JoinDate = DateTime.UtcNow, IsActive = false });
            await db.SaveChangesAsync();

            var handler = new GetEmployeeReportQueryHandler(new ReportRepository(db));
            var result = await handler.Handle(new GetEmployeeReportQuery(), CancellationToken.None);

            Assert.Equal(3, result.TotalEmployees);
            Assert.Equal(2, result.ActiveEmployees);
            Assert.Equal(1, result.InactiveEmployees);
        }

        [Fact]
        public async Task GetDepartmentCounts_GroupsEmployeesByDepartment()
        {
            using var db = CreateDb();
            var dept = new Department { Id = Guid.NewGuid(), Name = "Engineering", CreatedAtUtc = DateTime.UtcNow };
            await db.Departments.AddAsync(dept);
            await db.Employees.AddRangeAsync(
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "One", JoinDate = DateTime.UtcNow, DepartmentId = dept.Id, IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "B", LastName = "Two", JoinDate = DateTime.UtcNow, DepartmentId = dept.Id, IsActive = true });
            await db.SaveChangesAsync();

            var handler = new GetDepartmentCountsQueryHandler(new ReportRepository(db));
            var result = (await handler.Handle(new GetDepartmentCountsQuery(), CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal("Engineering", result[0].DepartmentName);
            Assert.Equal(2, result[0].EmployeeCount);
        }

        [Fact]
        public async Task GetDepartmentCounts_ExcludesSoftDeletedDepartments()
        {
            using var db = CreateDb();
            await db.Departments.AddRangeAsync(
                new Department { Id = Guid.NewGuid(), Name = "Active Dept", CreatedAtUtc = DateTime.UtcNow, IsDeleted = false },
                new Department { Id = Guid.NewGuid(), Name = "Deleted Dept", CreatedAtUtc = DateTime.UtcNow, IsDeleted = true });
            await db.SaveChangesAsync();

            var handler = new GetDepartmentCountsQueryHandler(new ReportRepository(db));
            var result = (await handler.Handle(new GetDepartmentCountsQuery(), CancellationToken.None)).ToList();

            Assert.Single(result);
            Assert.Equal("Active Dept", result[0].DepartmentName);
        }

        [Fact]
        public async Task GetLeaveSummary_CountsByStatusWithinRange()
        {
            using var db = CreateDb();
            var now = DateTime.UtcNow;
            await db.LeaveRequests.AddRangeAsync(
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), LeaveTypeId = Guid.NewGuid(), StartDate = now, EndDate = now, TotalDays = 1, Status = LeaveStatus.Pending, CreatedAtUtc = now },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), LeaveTypeId = Guid.NewGuid(), StartDate = now, EndDate = now, TotalDays = 1, Status = LeaveStatus.Approved, CreatedAtUtc = now },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), LeaveTypeId = Guid.NewGuid(), StartDate = now, EndDate = now, TotalDays = 1, Status = LeaveStatus.Rejected, CreatedAtUtc = now },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), LeaveTypeId = Guid.NewGuid(), StartDate = now, EndDate = now, TotalDays = 1, Status = LeaveStatus.Approved, CreatedAtUtc = now.AddYears(-2) }); // outside range
            await db.SaveChangesAsync();

            var handler = new GetLeaveSummaryQueryHandler(new ReportRepository(db));
            var result = await handler.Handle(new GetLeaveSummaryQuery { From = now.AddDays(-1), To = now.AddDays(1) }, CancellationToken.None);

            Assert.Equal(3, result.TotalRequests);
            Assert.Equal(1, result.Pending);
            Assert.Equal(1, result.Approved);
            Assert.Equal(1, result.Rejected);
        }

        [Fact]
        public async Task GetEmployeeJoinExit_ReturnsJoinersAndLeaversInRange()
        {
            using var db = CreateDb();
            var now = DateTime.UtcNow.Date;
            await db.Employees.AddRangeAsync(
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Joiner", LastName = "InRange", JoinDate = now, IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "Joiner", LastName = "OutOfRange", JoinDate = now.AddYears(-5), IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E3", FirstName = "Leaver", LastName = "InRange", JoinDate = now.AddYears(-1), ExitDate = now, IsActive = false });
            await db.SaveChangesAsync();

            var handler = new GetEmployeeJoinExitQueryHandler(new ReportRepository(db));
            var result = (await handler.Handle(new GetEmployeeJoinExitQuery { From = now.AddDays(-1), To = now.AddDays(1) }, CancellationToken.None)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.EmployeeName == "Joiner InRange");
            Assert.Contains(result, r => r.EmployeeName == "Leaver InRange");
        }

        [Fact]
        public async Task ExportDepartmentCounts_ProducesCsvWithHeaderAndRows()
        {
            using var db = CreateDb();
            var dept = new Department { Id = Guid.NewGuid(), Name = "Sales", CreatedAtUtc = DateTime.UtcNow };
            await db.Departments.AddAsync(dept);
            await db.Employees.AddAsync(new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "One", JoinDate = DateTime.UtcNow, DepartmentId = dept.Id, IsActive = true });
            await db.SaveChangesAsync();

            var handler = new ExportDepartmentCountsQueryHandler(new ReportRepository(db), new CsvExportService());
            var result = await handler.Handle(new ExportDepartmentCountsQuery(), CancellationToken.None);

            var csv = Encoding.UTF8.GetString(result.Content);
            Assert.Equal("text/csv", result.ContentType);
            Assert.Contains("DepartmentId,DepartmentName,EmployeeCount", csv);
            Assert.Contains("Sales,1", csv);
        }

        [Fact]
        public async Task ExportDepartmentCounts_NeutralizesFormulaInjectionInDepartmentName()
        {
            using var db = CreateDb();
            await db.Departments.AddAsync(new Department { Id = Guid.NewGuid(), Name = "=cmd|'/c calc'!A1", CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var handler = new ExportDepartmentCountsQueryHandler(new ReportRepository(db), new CsvExportService());
            var result = await handler.Handle(new ExportDepartmentCountsQuery(), CancellationToken.None);

            var csv = Encoding.UTF8.GetString(result.Content);
            Assert.DoesNotContain(",=cmd", csv);
            Assert.Contains("'=cmd|'/c calc'!A1", csv);
        }

        [Fact]
        public async Task ExportEmployeeJoinExit_ProducesCsvWithHeaderAndRows()
        {
            using var db = CreateDb();
            var now = DateTime.UtcNow.Date;
            await db.Employees.AddAsync(new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "Jane", LastName = "Doe", JoinDate = now, IsActive = true });
            await db.SaveChangesAsync();

            var handler = new ExportEmployeeJoinExitQueryHandler(new ReportRepository(db), new CsvExportService());
            var result = await handler.Handle(new ExportEmployeeJoinExitQuery { From = now.AddDays(-1), To = now.AddDays(1) }, CancellationToken.None);

            var csv = Encoding.UTF8.GetString(result.Content);
            Assert.Contains("EmployeeId,EmployeeName,JoinDate,ExitDate", csv);
            Assert.Contains("Jane Doe", csv);
        }

        [Theory]
        [InlineData(-1, 1, true)]   // valid range
        [InlineData(1, -1, false)]  // from after to
        public void GetLeaveSummaryQueryValidator_EnforcesRange(int fromOffsetDays, int toOffsetDays, bool expectedValid)
        {
            var validator = new GetLeaveSummaryQueryValidator();
            var query = new GetLeaveSummaryQuery { From = DateTime.UtcNow.AddDays(fromOffsetDays), To = DateTime.UtcNow.AddDays(toOffsetDays) };
            Assert.Equal(expectedValid, validator.Validate(query).IsValid);
        }

        [Fact]
        public void GetLeaveSummaryQueryValidator_MissingDates_IsInvalid()
        {
            var validator = new GetLeaveSummaryQueryValidator();
            Assert.False(validator.Validate(new GetLeaveSummaryQuery()).IsValid);
        }

        [Theory]
        [InlineData(-1, 1, true)]
        [InlineData(1, -1, false)]
        public void GetEmployeeJoinExitQueryValidator_EnforcesRange(int fromOffsetDays, int toOffsetDays, bool expectedValid)
        {
            var validator = new GetEmployeeJoinExitQueryValidator();
            var query = new GetEmployeeJoinExitQuery { From = DateTime.UtcNow.AddDays(fromOffsetDays), To = DateTime.UtcNow.AddDays(toOffsetDays) };
            Assert.Equal(expectedValid, validator.Validate(query).IsValid);
        }
    }
}

using EMS.Application.Features.Dashboard.Handlers;
using EMS.Application.Features.Dashboard.Queries;
using EMS.Application.Features.Dashboard.Validators;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class DashboardTests
    {
        private static ApplicationDbContext CreateDb(string name) =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options);

        [Fact]
        public async Task GetSummaryAsync_ComputesEmployeeLeaveAndDepartmentCounts()
        {
            using var db = CreateDb("ems_dashboard_test_" + Guid.NewGuid());

            var dept = new Department { Id = Guid.NewGuid(), Name = "Engineering", CreatedAtUtc = DateTime.UtcNow };
            await db.Departments.AddAsync(dept);

            var activeEmp = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "B", IsActive = true, DepartmentId = dept.Id, JoinDate = DateTime.UtcNow };
            var inactiveEmp = new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "C", LastName = "D", IsActive = false, DepartmentId = dept.Id, JoinDate = DateTime.UtcNow };
            await db.Employees.AddRangeAsync(activeEmp, inactiveEmp);

            var today = DateTime.UtcNow.Date;
            await db.LeaveRequests.AddRangeAsync(
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = activeEmp.Id, LeaveTypeId = Guid.NewGuid(), Status = LeaveStatus.Pending, StartDate = today, EndDate = today, CreatedAtUtc = DateTime.UtcNow },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = activeEmp.Id, LeaveTypeId = Guid.NewGuid(), Status = LeaveStatus.Approved, StartDate = today, EndDate = today, DecisionAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow },
                new LeaveRequest { Id = Guid.NewGuid(), EmployeeId = activeEmp.Id, LeaveTypeId = Guid.NewGuid(), Status = LeaveStatus.Rejected, StartDate = today, EndDate = today, DecisionAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow });

            await db.SaveChangesAsync();

            var attendanceRepo = new AttendanceRepository(db);
            var repo = new DashboardRepository(db, attendanceRepo);

            var summary = await repo.GetSummaryAsync(null, today, CancellationToken.None);

            Assert.Equal(2, summary.TotalEmployees);
            Assert.Equal(1, summary.ActiveEmployees);
            Assert.Equal(1, summary.InactiveEmployees);
            Assert.Equal(1, summary.Leave.Pending);
            Assert.Equal(1, summary.Leave.ApprovedToday);
            Assert.Equal(1, summary.Leave.RejectedToday);

            var deptSummary = Assert.Single(summary.Departments);
            Assert.Equal(dept.Id, deptSummary.DepartmentId);
            Assert.Equal(1, deptSummary.ActiveEmployees);
        }

        [Fact]
        public async Task GetSummaryAsync_FiltersByDepartment()
        {
            using var db = CreateDb("ems_dashboard_test_" + Guid.NewGuid());

            var deptA = new Department { Id = Guid.NewGuid(), Name = "Engineering", CreatedAtUtc = DateTime.UtcNow };
            var deptB = new Department { Id = Guid.NewGuid(), Name = "Sales", CreatedAtUtc = DateTime.UtcNow };
            await db.Departments.AddRangeAsync(deptA, deptB);

            await db.Employees.AddRangeAsync(
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "B", IsActive = true, DepartmentId = deptA.Id, JoinDate = DateTime.UtcNow },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "C", LastName = "D", IsActive = true, DepartmentId = deptB.Id, JoinDate = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var attendanceRepo = new AttendanceRepository(db);
            var repo = new DashboardRepository(db, attendanceRepo);

            var summary = await repo.GetSummaryAsync(deptA.Id, DateTime.UtcNow.Date, CancellationToken.None);

            Assert.Equal(1, summary.TotalEmployees);
            var deptSummary = Assert.Single(summary.Departments);
            Assert.Equal(deptA.Id, deptSummary.DepartmentId);
        }

        [Fact]
        public async Task GetDailyCountsAsync_CountsAttendanceStatusesForTheGivenDate()
        {
            using var db = CreateDb("ems_attendance_test_" + Guid.NewGuid());
            var date = DateTime.UtcNow.Date;

            var emp1 = Guid.NewGuid();
            var emp2 = Guid.NewGuid();
            var emp3 = Guid.NewGuid();
            var emp4 = Guid.NewGuid();

            await db.AttendanceRecords.AddRangeAsync(
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = emp1, AttendanceDate = date, Status = AttendanceStatus.Present, IsLateArrival = false, CreatedAtUtc = DateTime.UtcNow },
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = emp2, AttendanceDate = date, Status = AttendanceStatus.Present, IsLateArrival = true, CreatedAtUtc = DateTime.UtcNow },
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = emp3, AttendanceDate = date, Status = AttendanceStatus.Absent, CreatedAtUtc = DateTime.UtcNow },
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = emp4, AttendanceDate = date, Status = AttendanceStatus.OnLeave, CreatedAtUtc = DateTime.UtcNow },
                // A different date must not be counted.
                new AttendanceRecord { Id = Guid.NewGuid(), EmployeeId = emp1, AttendanceDate = date.AddDays(-1), Status = AttendanceStatus.Absent, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var repo = new AttendanceRepository(db);
            var result = await repo.GetDailyCountsAsync(date, null, CancellationToken.None);

            Assert.Equal(2, result.Present);
            Assert.Equal(1, result.Absent);
            Assert.Equal(1, result.Late);
            Assert.Equal(1, result.OnLeave);
        }

        [Fact]
        public async Task Handle_DefaultsToTodayWhenDateNotProvided()
        {
            using var db = CreateDb("ems_dashboard_handler_test_" + Guid.NewGuid());
            var attendanceRepo = new AttendanceRepository(db);
            var repo = new DashboardRepository(db, attendanceRepo);
            var handler = new GetDashboardSummaryQueryHandler(repo, NullLogger<GetDashboardSummaryQueryHandler>.Instance);

            var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

            Assert.Equal(0, result.TotalEmployees);
        }

        [Fact]
        public void Validator_RejectsFutureDates()
        {
            var validator = new GetDashboardSummaryQueryValidator();
            var result = validator.Validate(new GetDashboardSummaryQuery { Date = DateTime.UtcNow.Date.AddDays(1) });
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validator_AllowsTodayAndPastDates()
        {
            var validator = new GetDashboardSummaryQueryValidator();
            var result = validator.Validate(new GetDashboardSummaryQuery { Date = DateTime.UtcNow.Date.AddDays(-1) });
            Assert.True(result.IsValid);
        }
    }
}

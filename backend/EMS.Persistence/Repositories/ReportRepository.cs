using EMS.Application.DTOs.Reports;
using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ApplicationDbContext _db;

        public ReportRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<EmployeeReportDto> GetEmployeeReportAsync()
        {
            var total = await _db.Employees.CountAsync();
            var active = await _db.Employees.CountAsync(e => e.IsActive);
            var inactive = total - active;
            return new EmployeeReportDto { TotalEmployees = total, ActiveEmployees = active, InactiveEmployees = inactive };
        }

        public async Task<IEnumerable<DepartmentCountDto>> GetDepartmentCountsAsync()
        {
            var q = await _db.Departments
                .GroupJoin(_db.Employees, d => d.Id, e => e.DepartmentId, (d, emps) => new DepartmentCountDto
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    EmployeeCount = emps.Count()
                })
                .ToListAsync();
            return q;
        }

        public async Task<IEnumerable<EmployeeJoinExitDto>> GetEmployeeJoinExitAsync(DateTime from, DateTime to)
        {
            var q = await _db.Employees
                .Where(e => e.JoinDate >= from && e.JoinDate <= to || (e.ExitDate.HasValue && e.ExitDate >= from && e.ExitDate <= to))
                .Select(e => new EmployeeJoinExitDto
                {
                    EmployeeId = e.Id,
                    EmployeeName = e.FirstName + " " + e.LastName,
                    JoinDate = e.JoinDate,
                    ExitDate = e.ExitDate
                })
                .ToListAsync();
            return q;
        }

        public async Task<LeaveSummaryReportDto> GetLeaveSummaryAsync(DateTime from, DateTime to)
        {
            var q = _db.LeaveRequests.Where(l => l.CreatedAtUtc >= from && l.CreatedAtUtc <= to);
            var total = await q.CountAsync();
            var pending = await q.CountAsync(l => l.Status == EMS.Domain.Enums.LeaveStatus.Pending);
            var approved = await q.CountAsync(l => l.Status == EMS.Domain.Enums.LeaveStatus.Approved);
            var rejected = await q.CountAsync(l => l.Status == EMS.Domain.Enums.LeaveStatus.Rejected);
            return new LeaveSummaryReportDto { TotalRequests = total, Pending = pending, Approved = approved, Rejected = rejected };
        }

        public async Task<DashboardReportsDto> GetDashboardReportsAsync(DateTime asOf)
        {
            var empReport = await GetEmployeeReportAsync();
            var leave = await GetLeaveSummaryAsync(asOf.Date.AddDays(-30), asOf.Date);
            return new DashboardReportsDto
            {
                Headcount = empReport.TotalEmployees,
                Active = empReport.ActiveEmployees,
                Inactive = empReport.InactiveEmployees,
                PresentToday = 0, // attendance not implemented yet
                AbsentToday = 0,
                LeaveSummary = leave
            };
        }
    }
}

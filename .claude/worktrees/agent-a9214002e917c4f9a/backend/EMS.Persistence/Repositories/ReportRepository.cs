using EMS.Application.DTOs.Reports;
using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public async Task<EmployeeReportDto> GetEmployeeReportAsync(CancellationToken ct = default)
        {
            var total = await _db.Employees.CountAsync(ct);
            var active = await _db.Employees.CountAsync(e => e.IsActive, ct);
            var inactive = total - active;
            return new EmployeeReportDto { TotalEmployees = total, ActiveEmployees = active, InactiveEmployees = inactive };
        }

        public async Task<IEnumerable<DepartmentCountDto>> GetDepartmentCountsAsync(CancellationToken ct = default)
        {
            var q = await _db.Departments
                .Where(d => !d.IsDeleted)
                .GroupJoin(_db.Employees, d => d.Id, e => e.DepartmentId, (d, emps) => new DepartmentCountDto
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    EmployeeCount = emps.Count()
                })
                .ToListAsync(ct);
            return q;
        }

        public async Task<IEnumerable<EmployeeJoinExitDto>> GetEmployeeJoinExitAsync(DateTime from, DateTime to, CancellationToken ct = default)
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
                .ToListAsync(ct);
            return q;
        }

        public async Task<LeaveSummaryReportDto> GetLeaveSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var q = _db.LeaveRequests.Where(l => l.CreatedAtUtc >= from && l.CreatedAtUtc <= to);
            var total = await q.CountAsync(ct);
            var pending = await q.CountAsync(l => l.Status == EMS.Domain.Enums.LeaveStatus.Pending, ct);
            var approved = await q.CountAsync(l => l.Status == EMS.Domain.Enums.LeaveStatus.Approved, ct);
            var rejected = await q.CountAsync(l => l.Status == EMS.Domain.Enums.LeaveStatus.Rejected, ct);
            return new LeaveSummaryReportDto { TotalRequests = total, Pending = pending, Approved = approved, Rejected = rejected };
        }
    }
}

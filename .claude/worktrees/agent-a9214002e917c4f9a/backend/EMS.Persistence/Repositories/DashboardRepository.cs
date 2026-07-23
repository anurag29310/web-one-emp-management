using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IAttendanceRepository _attendanceRepository;

        public DashboardRepository(ApplicationDbContext db, IAttendanceRepository attendanceRepository)
        {
            _db = db;
            _attendanceRepository = attendanceRepository;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(Guid? departmentId, DateTime date, CancellationToken ct = default)
        {
            var employees = _db.Employees.AsNoTracking().AsQueryable();
            if (departmentId.HasValue)
                employees = employees.Where(e => e.DepartmentId == departmentId.Value);

            var total = await employees.CountAsync(ct);
            var active = await employees.CountAsync(e => e.IsActive, ct);
            var inactive = total - active;

            var leaveRequests = _db.LeaveRequests.AsNoTracking().AsQueryable();
            if (departmentId.HasValue)
            {
                leaveRequests = leaveRequests.Where(l =>
                    _db.Employees.Any(e => e.Id == l.EmployeeId && e.DepartmentId == departmentId.Value));
            }

            var pending = await leaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending, ct);
            var approvedToday = await leaveRequests.CountAsync(l =>
                l.Status == LeaveStatus.Approved && l.DecisionAtUtc.HasValue && l.DecisionAtUtc.Value.Date == date, ct);
            var rejectedToday = await leaveRequests.CountAsync(l =>
                l.Status == LeaveStatus.Rejected && l.DecisionAtUtc.HasValue && l.DecisionAtUtc.Value.Date == date, ct);

            var departments = await _db.Departments.AsNoTracking()
                .Where(d => !d.IsDeleted && (!departmentId.HasValue || d.Id == departmentId.Value))
                .Select(d => new DepartmentSummaryDto
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    ActiveEmployees = _db.Employees.Count(e => e.DepartmentId == d.Id && e.IsActive)
                })
                .ToListAsync(ct);

            var attendance = await _attendanceRepository.GetDailyCountsAsync(date, departmentId, ct);

            return new DashboardSummaryDto
            {
                TotalEmployees = total,
                ActiveEmployees = active,
                InactiveEmployees = inactive,
                Attendance = attendance,
                Leave = new LeaveSummaryDto
                {
                    Pending = pending,
                    ApprovedToday = approvedToday,
                    RejectedToday = rejectedToday
                },
                Departments = departments
            };
        }
    }
}

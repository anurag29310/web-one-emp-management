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
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _db;

        public AttendanceRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<AttendanceSummaryDto> GetDailyCountsAsync(DateTime date, Guid? departmentId, CancellationToken ct = default)
        {
            var query = _db.AttendanceRecords.AsNoTracking()
                .Where(a => !a.IsDeleted && a.AttendanceDate == date.Date);

            if (departmentId.HasValue)
            {
                query = query.Where(a => _db.Employees.Any(e => e.Id == a.EmployeeId && e.DepartmentId == departmentId.Value));
            }

            var present = await query.CountAsync(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.HalfDay, ct);
            var absent = await query.CountAsync(a => a.Status == AttendanceStatus.Absent, ct);
            var onLeave = await query.CountAsync(a => a.Status == AttendanceStatus.OnLeave, ct);

            // IsLateArrival is a flag on an otherwise Present/HalfDay record, not a separate
            // status, so "late" overlaps with "present" by design (matches the dashboard's
            // documented sample response, where the four counts are not mutually exclusive).
            var late = await query.CountAsync(a => a.IsLateArrival, ct);

            return new AttendanceSummaryDto
            {
                Present = present,
                Absent = absent,
                Late = late,
                OnLeave = onLeave
            };
        }
    }
}

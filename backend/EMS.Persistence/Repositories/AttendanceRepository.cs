using EMS.Application.DTOs;
using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        // ─── Dashboard ─────────────────────────────────────────────────────────────

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

        // ─── Attendance records ─────────────────────────────────────────────────────

        public async Task<AttendanceRecord?> GetRecordByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);

        public async Task<AttendanceRecord?> GetRecordByEmployeeAndDateAsync(Guid employeeId, DateTime date, CancellationToken ct = default) =>
            await _db.AttendanceRecords.FirstOrDefaultAsync(
                a => a.EmployeeId == employeeId && a.AttendanceDate == date.Date && !a.IsDeleted, ct);

        public async Task<IEnumerable<AttendanceRecord>> GetRecordsAsync(AttendanceRecordFilter filter, int page, int pageSize, CancellationToken ct = default) =>
            await BuildRecordFilter(filter)
                .OrderByDescending(a => a.AttendanceDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountRecordsAsync(AttendanceRecordFilter filter, CancellationToken ct = default) =>
            await BuildRecordFilter(filter).CountAsync(ct);

        public async Task<IEnumerable<AttendanceRecord>> GetAllRecordsAsync(AttendanceRecordFilter filter, CancellationToken ct = default) =>
            await BuildRecordFilter(filter).OrderByDescending(a => a.AttendanceDate).ToListAsync(ct);

        public async Task AddRecordAsync(AttendanceRecord record, CancellationToken ct = default) =>
            await _db.AttendanceRecords.AddAsync(record, ct);

        public Task UpdateRecordAsync(AttendanceRecord record, CancellationToken ct = default)
        {
            _db.AttendanceRecords.Update(record);
            return Task.CompletedTask;
        }

        public Task DeleteRecordAsync(AttendanceRecord record, CancellationToken ct = default)
        {
            record.IsDeleted = true;
            _db.AttendanceRecords.Update(record);
            return Task.CompletedTask;
        }

        // ─── Attendance corrections ─────────────────────────────────────────────────

        public async Task<AttendanceCorrection?> GetCorrectionByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.AttendanceCorrections.Include(c => c.AttendanceRecord)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        public async Task<IEnumerable<AttendanceCorrection>> GetCorrectionsAsync(
            int page, int pageSize, Guid? employeeId, IEnumerable<Guid>? employeeIdsScope, string? status, CancellationToken ct = default) =>
            await BuildCorrectionFilter(employeeId, employeeIdsScope, status)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountCorrectionsAsync(Guid? employeeId, IEnumerable<Guid>? employeeIdsScope, string? status, CancellationToken ct = default) =>
            await BuildCorrectionFilter(employeeId, employeeIdsScope, status).CountAsync(ct);

        public async Task AddCorrectionAsync(AttendanceCorrection correction, CancellationToken ct = default) =>
            await _db.AttendanceCorrections.AddAsync(correction, ct);

        public Task UpdateCorrectionAsync(AttendanceCorrection correction, CancellationToken ct = default)
        {
            _db.AttendanceCorrections.Update(correction);
            return Task.CompletedTask;
        }

        // ─── Shifts ─────────────────────────────────────────────────────────────────

        public async Task<Shift?> GetShiftByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Shifts.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

        public async Task<IEnumerable<Shift>> GetShiftsAsync(CancellationToken ct = default) =>
            await _db.Shifts.AsNoTracking().Where(s => !s.IsDeleted).OrderBy(s => s.Name).ToListAsync(ct);

        public async Task AddShiftAsync(Shift shift, CancellationToken ct = default) =>
            await _db.Shifts.AddAsync(shift, ct);

        public Task UpdateShiftAsync(Shift shift, CancellationToken ct = default)
        {
            _db.Shifts.Update(shift);
            return Task.CompletedTask;
        }

        public Task DeleteShiftAsync(Shift shift, CancellationToken ct = default)
        {
            shift.IsDeleted = true;
            _db.Shifts.Update(shift);
            return Task.CompletedTask;
        }

        // ─── Employee shift assignments ─────────────────────────────────────────────

        public async Task<EmployeeShift?> GetEmployeeShiftByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.EmployeeShifts.FirstOrDefaultAsync(es => es.Id == id && !es.IsDeleted, ct);

        public async Task<IEnumerable<EmployeeShift>> GetEmployeeShiftsAsync(Guid employeeId, CancellationToken ct = default) =>
            await _db.EmployeeShifts.AsNoTracking()
                .Where(es => es.EmployeeId == employeeId && !es.IsDeleted)
                .OrderByDescending(es => es.EffectiveFrom)
                .ToListAsync(ct);

        public async Task<EmployeeShift?> GetActiveEmployeeShiftAsync(Guid employeeId, DateTime onDate, CancellationToken ct = default)
        {
            var date = onDate.Date;
            return await _db.EmployeeShifts.AsNoTracking()
                .Where(es => es.EmployeeId == employeeId && !es.IsDeleted
                    && es.EffectiveFrom <= date && (es.EffectiveTo == null || es.EffectiveTo >= date))
                .OrderByDescending(es => es.EffectiveFrom)
                .FirstOrDefaultAsync(ct);
        }

        public async Task AddEmployeeShiftAsync(EmployeeShift assignment, CancellationToken ct = default) =>
            await _db.EmployeeShifts.AddAsync(assignment, ct);

        public Task UpdateEmployeeShiftAsync(EmployeeShift assignment, CancellationToken ct = default)
        {
            _db.EmployeeShifts.Update(assignment);
            return Task.CompletedTask;
        }

        public Task DeleteEmployeeShiftAsync(EmployeeShift assignment, CancellationToken ct = default)
        {
            assignment.IsDeleted = true;
            _db.EmployeeShifts.Update(assignment);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private IQueryable<AttendanceRecord> BuildRecordFilter(AttendanceRecordFilter filter)
        {
            var q = _db.AttendanceRecords.AsNoTracking().Where(a => !a.IsDeleted);

            if (filter.EmployeeId.HasValue)
                q = q.Where(a => a.EmployeeId == filter.EmployeeId.Value);
            else if (filter.EmployeeIdsScope != null)
                q = q.Where(a => filter.EmployeeIdsScope.Contains(a.EmployeeId));

            if (filter.DepartmentId.HasValue)
                q = q.Where(a => _db.Employees.Any(e => e.Id == a.EmployeeId && e.DepartmentId == filter.DepartmentId.Value));

            if (filter.DateFrom.HasValue)
                q = q.Where(a => a.AttendanceDate >= filter.DateFrom.Value.Date);

            if (filter.DateTo.HasValue)
                q = q.Where(a => a.AttendanceDate <= filter.DateTo.Value.Date);

            if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<AttendanceStatus>(filter.Status, true, out var parsedStatus))
                q = q.Where(a => a.Status == parsedStatus);

            if (filter.IsLateArrival.HasValue)
                q = q.Where(a => a.IsLateArrival == filter.IsLateArrival.Value);

            if (filter.IsEarlyLeave.HasValue)
                q = q.Where(a => a.IsEarlyLeave == filter.IsEarlyLeave.Value);

            return q;
        }

        private IQueryable<AttendanceCorrection> BuildCorrectionFilter(Guid? employeeId, IEnumerable<Guid>? employeeIdsScope, string? status)
        {
            var q = _db.AttendanceCorrections.AsNoTracking().Where(c => !c.IsDeleted);

            if (employeeId.HasValue)
                q = q.Where(c => c.RequestedByEmployeeId == employeeId.Value);
            else if (employeeIdsScope != null)
                q = q.Where(c => employeeIdsScope.Contains(c.RequestedByEmployeeId));

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CorrectionStatus>(status, true, out var parsedStatus))
                q = q.Where(c => c.Status == parsedStatus);

            return q;
        }
    }
}

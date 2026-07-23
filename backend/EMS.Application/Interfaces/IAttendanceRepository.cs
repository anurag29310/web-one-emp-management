using EMS.Application.DTOs;
using EMS.Application.Features.Attendance.DTOs;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IAttendanceRepository
    {
        // ─── Dashboard ─────────────────────────────────────────────────────────────
        Task<AttendanceSummaryDto> GetDailyCountsAsync(DateTime date, Guid? departmentId, CancellationToken ct = default);

        // ─── Attendance records ────────────────────────────────────────────────────
        Task<AttendanceRecord?> GetRecordByIdAsync(Guid id, CancellationToken ct = default);
        Task<AttendanceRecord?> GetRecordByEmployeeAndDateAsync(Guid employeeId, DateTime date, CancellationToken ct = default);
        Task<IEnumerable<AttendanceRecord>> GetRecordsAsync(AttendanceRecordFilter filter, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountRecordsAsync(AttendanceRecordFilter filter, CancellationToken ct = default);
        Task<IEnumerable<AttendanceRecord>> GetAllRecordsAsync(AttendanceRecordFilter filter, CancellationToken ct = default);
        Task AddRecordAsync(AttendanceRecord record, CancellationToken ct = default);
        Task UpdateRecordAsync(AttendanceRecord record, CancellationToken ct = default);
        Task DeleteRecordAsync(AttendanceRecord record, CancellationToken ct = default);

        // ─── Attendance corrections ────────────────────────────────────────────────
        Task<AttendanceCorrection?> GetCorrectionByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<AttendanceCorrection>> GetCorrectionsAsync(int page, int pageSize, Guid? employeeId, IEnumerable<Guid>? employeeIdsScope, string? status, CancellationToken ct = default);
        Task<int> CountCorrectionsAsync(Guid? employeeId, IEnumerable<Guid>? employeeIdsScope, string? status, CancellationToken ct = default);
        Task AddCorrectionAsync(AttendanceCorrection correction, CancellationToken ct = default);
        Task UpdateCorrectionAsync(AttendanceCorrection correction, CancellationToken ct = default);

        // ─── Shifts ─────────────────────────────────────────────────────────────────
        Task<Shift?> GetShiftByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Shift>> GetShiftsAsync(CancellationToken ct = default);
        Task AddShiftAsync(Shift shift, CancellationToken ct = default);
        Task UpdateShiftAsync(Shift shift, CancellationToken ct = default);
        Task DeleteShiftAsync(Shift shift, CancellationToken ct = default);

        // ─── Employee shift assignments ────────────────────────────────────────────
        Task<EmployeeShift?> GetEmployeeShiftByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<EmployeeShift>> GetEmployeeShiftsAsync(Guid employeeId, CancellationToken ct = default);
        Task<EmployeeShift?> GetActiveEmployeeShiftAsync(Guid employeeId, DateTime onDate, CancellationToken ct = default);
        Task AddEmployeeShiftAsync(EmployeeShift assignment, CancellationToken ct = default);
        Task UpdateEmployeeShiftAsync(EmployeeShift assignment, CancellationToken ct = default);
        Task DeleteEmployeeShiftAsync(EmployeeShift assignment, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

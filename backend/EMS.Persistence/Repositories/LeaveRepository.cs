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
    public class LeaveRepository : ILeaveRepository
    {
        private readonly ApplicationDbContext _db;

        public LeaveRepository(ApplicationDbContext db) => _db = db;

        public async Task<LeaveRequest?> GetLeaveByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id, ct);

        public async Task<IEnumerable<LeaveRequest>> GetLeavesAsync(
            int page, int pageSize, Guid? employeeId, Guid? leaveTypeId, int? year, string? status, CancellationToken ct = default)
        {
            var q = BuildLeaveFilter(employeeId, leaveTypeId, year, status);
            return await q.OrderByDescending(l => l.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task<int> CountLeavesAsync(
            Guid? employeeId, Guid? leaveTypeId, int? year, string? status, CancellationToken ct = default) =>
            await BuildLeaveFilter(employeeId, leaveTypeId, year, status).CountAsync(ct);

        public async Task AddLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default) =>
            await _db.LeaveRequests.AddAsync(request, ct);

        public Task UpdateLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default)
        {
            _db.LeaveRequests.Update(request);
            return Task.CompletedTask;
        }

        public Task ApproveLeaveAsync(LeaveRequest request, CancellationToken ct = default)
        {
            request.Status = LeaveStatus.Approved;
            request.DecisionAtUtc = DateTime.UtcNow;
            _db.LeaveRequests.Update(request);
            return Task.CompletedTask;
        }

        public Task RejectLeaveAsync(LeaveRequest request, CancellationToken ct = default)
        {
            request.Status = LeaveStatus.Rejected;
            request.DecisionAtUtc = DateTime.UtcNow;
            _db.LeaveRequests.Update(request);
            return Task.CompletedTask;
        }

        public Task CancelLeaveAsync(LeaveRequest request, CancellationToken ct = default)
        {
            request.Status = LeaveStatus.Cancelled;
            request.DecisionAtUtc = DateTime.UtcNow;
            _db.LeaveRequests.Update(request);
            return Task.CompletedTask;
        }

        public async Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken ct = default) =>
            await _db.LeaveBalances.FirstOrDefaultAsync(
                b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year, ct);

        public async Task<IEnumerable<LeaveBalance>> GetLeaveBalancesForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
            await _db.LeaveBalances.AsNoTracking()
                .Where(b => b.EmployeeId == employeeId)
                .ToListAsync(ct);

        public Task UpdateLeaveBalanceAsync(LeaveBalance balance, CancellationToken ct = default)
        {
            _db.LeaveBalances.Update(balance);
            return Task.CompletedTask;
        }

        public async Task<Holiday?> GetHolidayByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Holidays.FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, ct);

        public async Task<IEnumerable<Holiday>> GetHolidaysAsync(Guid? officeLocationId, int? year, bool? isOptional, CancellationToken ct = default)
        {
            var q = _db.Holidays.AsNoTracking().Where(h => !h.IsDeleted);

            if (officeLocationId.HasValue) q = q.Where(h => h.OfficeLocationId == officeLocationId.Value);
            if (year.HasValue) q = q.Where(h => h.HolidayDate.Year == year.Value);
            if (isOptional.HasValue) q = q.Where(h => h.IsOptional == isOptional.Value);

            return await q.OrderBy(h => h.HolidayDate).ToListAsync(ct);
        }

        public async Task AddHolidayAsync(Holiday holiday, CancellationToken ct = default) =>
            await _db.Holidays.AddAsync(holiday, ct);

        public Task UpdateHolidayAsync(Holiday holiday, CancellationToken ct = default)
        {
            _db.Holidays.Update(holiday);
            return Task.CompletedTask;
        }

        public Task DeleteHolidayAsync(Holiday holiday, CancellationToken ct = default)
        {
            holiday.IsDeleted = true;
            _db.Holidays.Update(holiday);
            return Task.CompletedTask;
        }

        public async Task<LeaveType?> GetLeaveTypeByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.Id == id && !lt.IsDeleted, ct);

        public async Task<LeaveType?> GetLeaveTypeByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.LeaveTypes.FirstOrDefaultAsync(lt => lt.Id == id, ct);

        public async Task<IEnumerable<LeaveType>> GetLeaveTypesAsync(CancellationToken ct = default) =>
            await _db.LeaveTypes.AsNoTracking().Where(lt => !lt.IsDeleted).OrderBy(lt => lt.Name).ToListAsync(ct);

        public async Task AddLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default) =>
            await _db.LeaveTypes.AddAsync(leaveType, ct);

        public Task UpdateLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default)
        {
            _db.LeaveTypes.Update(leaveType);
            return Task.CompletedTask;
        }

        public Task DeleteLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default)
        {
            leaveType.IsDeleted = true;
            _db.LeaveTypes.Update(leaveType);
            return Task.CompletedTask;
        }

        public Task RestoreLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default)
        {
            leaveType.IsDeleted = false;
            _db.LeaveTypes.Update(leaveType);
            return Task.CompletedTask;
        }

        public async Task<bool> LeaveTypeCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.LeaveTypes.AnyAsync(lt => lt.Code == code && !lt.IsDeleted && (excludeId == null || lt.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private IQueryable<LeaveRequest> BuildLeaveFilter(Guid? employeeId, Guid? leaveTypeId, int? year, string? status)
        {
            var q = _db.LeaveRequests.AsNoTracking().AsQueryable();

            if (employeeId.HasValue) q = q.Where(l => l.EmployeeId == employeeId.Value);
            if (leaveTypeId.HasValue) q = q.Where(l => l.LeaveTypeId == leaveTypeId.Value);
            if (year.HasValue) q = q.Where(l => l.StartDate.Year == year.Value || l.EndDate.Year == year.Value);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveStatus>(status, true, out var parsed))
                q = q.Where(l => l.Status == parsed);

            return q;
        }
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly ApplicationDbContext _db;

        public LeaveRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddLeaveRequestAsync(LeaveRequest request)
        {
            await _db.LeaveRequests.AddAsync(request);
        }

        public async Task ApproveLeaveAsync(LeaveRequest request)
        {
            request.Status = LeaveStatus.Approved;
            request.DecisionAtUtc = DateTime.UtcNow;
            _db.LeaveRequests.Update(request);
            await Task.CompletedTask;
        }

        public async Task<LeaveRequest?> GetLeaveByIdAsync(Guid id)
        {
            return await _db.LeaveRequests.FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeavesAsync(int page, int pageSize, Guid? employeeId, Guid? leaveTypeId, int? year, int? status)
        {
            var q = _db.LeaveRequests.AsNoTracking().AsQueryable();
            if (employeeId.HasValue) q = q.Where(l => l.EmployeeId == employeeId.Value);
            if (leaveTypeId.HasValue) q = q.Where(l => l.LeaveTypeId == leaveTypeId.Value);
            if (year.HasValue) q = q.Where(l => l.StartDate.Year == year.Value || l.EndDate.Year == year.Value);
            if (status.HasValue) q = q.Where(l => (int)l.Status == status.Value);

            q = q.OrderByDescending(l => l.CreatedAtUtc);
            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task RejectLeaveAsync(LeaveRequest request)
        {
            request.Status = LeaveStatus.Rejected;
            request.DecisionAtUtc = DateTime.UtcNow;
            _db.LeaveRequests.Update(request);
            await Task.CompletedTask;
        }

        public async Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year)
        {
            return await _db.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year);
        }

        public async Task<IEnumerable<LeaveBalance>> GetLeaveBalancesForEmployeeAsync(Guid employeeId)
        {
            return await _db.LeaveBalances.AsNoTracking().Where(b => b.EmployeeId == employeeId).ToListAsync();
        }

        public async Task<IEnumerable<Holiday>> GetHolidaysAsync(int year)
        {
            return await _db.Holidays.AsNoTracking().Where(h => h.HolidayDate.Year == year).ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}

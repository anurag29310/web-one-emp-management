using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface ILeaveRepository
    {
        Task<LeaveRequest?> GetLeaveByIdAsync(Guid id);
        Task<IEnumerable<LeaveRequest>> GetLeavesAsync(int page, int pageSize, Guid? employeeId, Guid? leaveTypeId, int? year, int? status);
        Task AddLeaveRequestAsync(LeaveRequest request);
        Task ApproveLeaveAsync(LeaveRequest request);
        Task RejectLeaveAsync(LeaveRequest request);
        Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year);
        Task<IEnumerable<LeaveBalance>> GetLeaveBalancesForEmployeeAsync(Guid employeeId);
        Task<IEnumerable<Holiday>> GetHolidaysAsync(int year);
        Task SaveChangesAsync();
    }
}

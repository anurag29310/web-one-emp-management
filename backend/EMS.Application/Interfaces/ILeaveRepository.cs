using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface ILeaveRepository
    {
        Task<LeaveRequest?> GetLeaveByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<LeaveRequest>> GetLeavesAsync(int page, int pageSize, Guid? employeeId, Guid? leaveTypeId, int? year, string? status, CancellationToken ct = default);
        Task<int> CountLeavesAsync(Guid? employeeId, Guid? leaveTypeId, int? year, string? status, CancellationToken ct = default);
        Task AddLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default);
        Task UpdateLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default);
        Task ApproveLeaveAsync(LeaveRequest request, CancellationToken ct = default);
        Task RejectLeaveAsync(LeaveRequest request, CancellationToken ct = default);
        Task CancelLeaveAsync(LeaveRequest request, CancellationToken ct = default);

        Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken ct = default);
        Task<IEnumerable<LeaveBalance>> GetLeaveBalancesForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
        Task UpdateLeaveBalanceAsync(LeaveBalance balance, CancellationToken ct = default);

        Task<IEnumerable<Holiday>> GetHolidaysAsync(int year, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

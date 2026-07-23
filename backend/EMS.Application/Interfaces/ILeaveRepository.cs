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
        Task<IEnumerable<LeaveRequest>> GetAllLeavesAsync(Guid? employeeId, Guid? leaveTypeId, int? year, string? status, CancellationToken ct = default);
        Task AddLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default);
        Task UpdateLeaveRequestAsync(LeaveRequest request, CancellationToken ct = default);
        Task ApproveLeaveAsync(LeaveRequest request, CancellationToken ct = default);
        Task RejectLeaveAsync(LeaveRequest request, CancellationToken ct = default);
        Task CancelLeaveAsync(LeaveRequest request, CancellationToken ct = default);

        Task<LeaveBalance?> GetLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken ct = default);
        Task<IEnumerable<LeaveBalance>> GetLeaveBalancesForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
        Task UpdateLeaveBalanceAsync(LeaveBalance balance, CancellationToken ct = default);

        Task<Holiday?> GetHolidayByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Holiday>> GetHolidaysAsync(Guid? officeLocationId, int? year, bool? isOptional, CancellationToken ct = default);
        Task AddHolidayAsync(Holiday holiday, CancellationToken ct = default);
        Task UpdateHolidayAsync(Holiday holiday, CancellationToken ct = default);
        Task DeleteHolidayAsync(Holiday holiday, CancellationToken ct = default);

        Task<LeaveType?> GetLeaveTypeByIdAsync(Guid id, CancellationToken ct = default);
        Task<LeaveType?> GetLeaveTypeByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<LeaveType>> GetLeaveTypesAsync(CancellationToken ct = default);
        Task AddLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default);
        Task UpdateLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default);
        Task DeleteLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default);
        Task RestoreLeaveTypeAsync(LeaveType leaveType, CancellationToken ct = default);
        Task<bool> LeaveTypeCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

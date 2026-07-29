using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance
{
    /// <summary>Shared manager-tier scoping checks for Goals/Reviews/Promotions, extending the same
    /// IsManager + GetDirectReportIdsAsync/IsDirectReportAsync pattern used by Attendance
    /// (GetAttendanceCorrectionsQueryHandler et al.) — factored out here because it repeats across
    /// ~10 handlers in this module rather than the 2-3 places it appears in Attendance.</summary>
    internal static class PerformanceScopeHelper
    {
        public static async Task<Guid?> ResolveRequesterEmployeeIdAsync(IAuthRepository authRepo, Guid requestingUserId, CancellationToken ct)
        {
            var requester = await authRepo.GetByIdAsync(requestingUserId, ct);
            return requester?.EmployeeId;
        }

        /// <summary>True if the requester is a Manager and targetEmployeeId is one of their direct reports.</summary>
        public static async Task<bool> IsManagerOfAsync(IEmployeeRepository employeeRepo, Guid? requesterEmployeeId, Guid targetEmployeeId, bool isManager, CancellationToken ct) =>
            isManager && requesterEmployeeId.HasValue &&
            await employeeRepo.IsDirectReportAsync(requesterEmployeeId.Value, targetEmployeeId, ct);
    }
}

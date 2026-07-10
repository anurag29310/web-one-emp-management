using EMS.Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<AttendanceSummaryDto> GetDailyCountsAsync(DateTime date, Guid? departmentId, CancellationToken ct = default);
    }
}

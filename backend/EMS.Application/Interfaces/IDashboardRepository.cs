using EMS.Application.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryDto> GetSummaryAsync(Guid? departmentId, DateTime date, CancellationToken ct = default);
    }
}

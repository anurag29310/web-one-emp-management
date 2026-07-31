using EMS.Application.DTOs;
using MediatR;

namespace EMS.Application.Features.PlatformDashboard.Queries
{
    public class GetPlatformDashboardSummaryQuery : IRequest<PlatformDashboardSummaryDto>
    {
        public int RecentCount { get; set; } = 5;
    }
}

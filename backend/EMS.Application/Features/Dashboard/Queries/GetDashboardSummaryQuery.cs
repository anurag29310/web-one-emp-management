using EMS.Application.DTOs;
using MediatR;

namespace EMS.Application.Features.Dashboard.Queries
{
    public class GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>
    {
    }
}

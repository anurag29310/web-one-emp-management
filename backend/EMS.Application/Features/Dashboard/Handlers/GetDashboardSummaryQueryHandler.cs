using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Dashboard.Handlers
{
    public class GetDashboardSummaryQueryHandler : IRequestHandler<Queries.GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IDashboardRepository _repo;

        public GetDashboardSummaryQueryHandler(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<DashboardSummaryDto> Handle(Queries.GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetSummaryAsync();
        }
    }
}

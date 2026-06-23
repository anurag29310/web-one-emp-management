using EMS.Application.DTOs.Reports;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Handlers
{
    public class GetLeaveSummaryQueryHandler : IRequestHandler<Queries.GetLeaveSummaryQuery, LeaveSummaryReportDto>
    {
        private readonly IReportRepository _repo;

        public GetLeaveSummaryQueryHandler(IReportRepository repo)
        {
            _repo = repo;
        }

        public async Task<LeaveSummaryReportDto> Handle(Queries.GetLeaveSummaryQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetLeaveSummaryAsync(request.From, request.To);
        }
    }
}

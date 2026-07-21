using EMS.Application.DTOs.Reports;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Handlers
{
    public class GetDepartmentCountsQueryHandler : IRequestHandler<Queries.GetDepartmentCountsQuery, IEnumerable<DepartmentCountDto>>
    {
        private readonly IReportRepository _repo;

        public GetDepartmentCountsQueryHandler(IReportRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<DepartmentCountDto>> Handle(Queries.GetDepartmentCountsQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetDepartmentCountsAsync(cancellationToken);
        }
    }
}

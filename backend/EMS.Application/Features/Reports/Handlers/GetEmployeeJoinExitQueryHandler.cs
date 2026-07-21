using EMS.Application.DTOs.Reports;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Handlers
{
    public class GetEmployeeJoinExitQueryHandler : IRequestHandler<Queries.GetEmployeeJoinExitQuery, IEnumerable<EmployeeJoinExitDto>>
    {
        private readonly IReportRepository _repo;

        public GetEmployeeJoinExitQueryHandler(IReportRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EmployeeJoinExitDto>> Handle(Queries.GetEmployeeJoinExitQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetEmployeeJoinExitAsync(request.From, request.To, cancellationToken);
        }
    }
}

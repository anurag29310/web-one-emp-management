using EMS.Application.DTOs.Reports;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reports.Handlers
{
    public class GetEmployeeReportQueryHandler : IRequestHandler<Queries.GetEmployeeReportQuery, EmployeeReportDto>
    {
        private readonly IReportRepository _repo;

        public GetEmployeeReportQueryHandler(IReportRepository repo)
        {
            _repo = repo;
        }

        public async Task<EmployeeReportDto> Handle(Queries.GetEmployeeReportQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetEmployeeReportAsync(cancellationToken);
        }
    }
}

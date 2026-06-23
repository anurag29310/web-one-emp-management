using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetReportingEmployeesQueryHandler : IRequestHandler<Queries.GetReportingEmployeesQuery, IEnumerable<EMS.Domain.Entities.Employee>>
    {
        private readonly IEmployeeRepository _repo;

        public GetReportingEmployeesQueryHandler(IEmployeeRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.Employee>> Handle(Queries.GetReportingEmployeesQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetReportingEmployeesAsync(request.ManagerId, request.Page, request.PageSize);
        }
    }
}

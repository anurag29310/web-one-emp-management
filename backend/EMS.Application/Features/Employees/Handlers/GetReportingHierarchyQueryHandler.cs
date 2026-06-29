using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetReportingHierarchyQueryHandler : IRequestHandler<Queries.GetReportingHierarchyQuery, IEnumerable<Employee>>
    {
        private readonly IEmployeeRepository _repo;

        public GetReportingHierarchyQueryHandler(IEmployeeRepository repo) => _repo = repo;

        public async Task<IEnumerable<Employee>> Handle(Queries.GetReportingHierarchyQuery request, CancellationToken cancellationToken) =>
            await _repo.GetManagerChainAsync(request.EmployeeId, cancellationToken);
    }
}

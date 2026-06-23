using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetEmployeesByDepartmentQueryHandler : IRequestHandler<Queries.GetEmployeesByDepartmentQuery, IEnumerable<EMS.Domain.Entities.Employee>>
    {
        private readonly IEmployeeRepository _repo;

        public GetEmployeesByDepartmentQueryHandler(IEmployeeRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.Employee>> Handle(Queries.GetEmployeesByDepartmentQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetByDepartmentAsync(request.DepartmentId, request.Page, request.PageSize);
        }
    }
}

using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetEmployeesQueryHandler : IRequestHandler<Queries.GetEmployeesQuery, IEnumerable<EMS.Domain.Entities.Employee>>
    {
        private readonly IEmployeeRepository _repo;

        public GetEmployeesQueryHandler(IEmployeeRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.Employee>> Handle(Queries.GetEmployeesQuery request, CancellationToken cancellationToken)
            => await _repo.GetAllAsync(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDir, request.DepartmentId, request.Status);
    }
}

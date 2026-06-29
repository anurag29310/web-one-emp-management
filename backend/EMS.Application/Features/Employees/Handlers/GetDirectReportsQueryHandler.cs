using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetDirectReportsQueryHandler : IRequestHandler<Queries.GetDirectReportsQuery, IEnumerable<Employee>>
    {
        private readonly IEmployeeRepository _repo;

        public GetDirectReportsQueryHandler(IEmployeeRepository repo) => _repo = repo;

        public async Task<IEnumerable<Employee>> Handle(Queries.GetDirectReportsQuery request, CancellationToken cancellationToken) =>
            await _repo.GetDirectReportsAsync(request.ManagerId, request.Page, request.PageSize, cancellationToken);
    }
}

using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Handlers
{
    public class GetEmployeesByTeamQueryHandler : IRequestHandler<Queries.GetEmployeesByTeamQuery, IEnumerable<EMS.Domain.Entities.Employee>>
    {
        private readonly IEmployeeRepository _repo;

        public GetEmployeesByTeamQueryHandler(IEmployeeRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<EMS.Domain.Entities.Employee>> Handle(Queries.GetEmployeesByTeamQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetByTeamAsync(request.TeamId, request.Page, request.PageSize, cancellationToken);
        }
    }
}

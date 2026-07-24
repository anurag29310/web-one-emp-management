using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Teams.Handlers
{
    public class GetTeamsByDepartmentQueryHandler : IRequestHandler<GetTeamsByDepartmentQuery, IEnumerable<Team>>
    {
        private readonly ITeamRepository _repo;

        public GetTeamsByDepartmentQueryHandler(ITeamRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Team>> Handle(GetTeamsByDepartmentQuery request, CancellationToken cancellationToken)
            => await _repo.GetByDepartmentAsync(request.DepartmentId, cancellationToken);
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Teams.Handlers
{
    public class GetTeamsQueryHandler : IRequestHandler<GetTeamsQuery, IEnumerable<Team>>
    {
        private readonly ITeamRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetTeamsQueryHandler(ITeamRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<Team>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
            => await _repo.GetAllAsync(_currentUser.CompanyId!.Value, cancellationToken);
    }
}

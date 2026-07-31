using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Teams.Handlers
{
    public class GetTeamByIdQueryHandler : IRequestHandler<GetTeamByIdQuery, Team?>
    {
        private readonly ITeamRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetTeamByIdQueryHandler(ITeamRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<Team?> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken);
    }
}

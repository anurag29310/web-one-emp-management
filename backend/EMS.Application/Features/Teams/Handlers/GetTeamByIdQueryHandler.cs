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

        public GetTeamByIdQueryHandler(ITeamRepository repo)
        {
            _repo = repo;
        }

        public async Task<Team?> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
            => await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}

using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Teams.Handlers
{
    public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand>
    {
        private readonly ITeamRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DeleteTeamCommandHandler> _logger;

        public DeleteTeamCommandHandler(ITeamRepository repo, ICurrentUserService currentUser, ILogger<DeleteTeamCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
        {
            var team = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Team {request.Id} not found.");

            team.DeletedAtUtc = DateTime.UtcNow;
            team.DeletedBy = _currentUser.UserId;

            await _repo.DeleteAsync(team, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) team {TeamId}", team.Id);
        }
    }
}

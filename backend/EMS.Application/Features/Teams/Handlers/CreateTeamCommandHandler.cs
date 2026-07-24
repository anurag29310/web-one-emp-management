using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Teams.Handlers
{
    public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, Team>
    {
        private readonly ITeamRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateTeamCommandHandler> _logger;

        public CreateTeamCommandHandler(ITeamRepository repo, ICurrentUserService currentUser, ILogger<CreateTeamCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Team> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
        {
            if (await _repo.CodeExistsAsync(request.DepartmentId, request.Code, ct: cancellationToken))
                throw new InvalidOperationException("Team code already exists in this department.");

            var team = new Team
            {
                Id = Guid.NewGuid(),
                DepartmentId = request.DepartmentId,
                Name = request.Name,
                Code = request.Code,
                LeadEmployeeId = request.LeadEmployeeId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            await _repo.AddAsync(team, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created team {TeamName} ({TeamId})", team.Name, team.Id);
            return team;
        }
    }
}

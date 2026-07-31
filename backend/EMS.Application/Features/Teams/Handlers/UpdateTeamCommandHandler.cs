using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Teams.Handlers
{
    public class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand, Team>
    {
        private readonly ITeamRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UpdateTeamCommandHandler> _logger;

        public UpdateTeamCommandHandler(ITeamRepository repo, ICurrentUserService currentUser, ILogger<UpdateTeamCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Team> Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            var team = await _repo.GetByIdAsync(request.Id, companyId, cancellationToken)
                ?? throw new InvalidOperationException($"Team {request.Id} not found.");

            if (await _repo.CodeExistsAsync(request.DepartmentId, request.Code, companyId, request.Id, cancellationToken))
                throw new InvalidOperationException("Team code already exists in this department.");

            team.DepartmentId = request.DepartmentId;
            team.Name = request.Name;
            team.Code = request.Code;
            team.LeadEmployeeId = request.LeadEmployeeId;
            team.UpdatedAtUtc = DateTime.UtcNow;
            team.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(team, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated team {TeamId}", team.Id);
            return team;
        }
    }
}

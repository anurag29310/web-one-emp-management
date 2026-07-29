using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class RestoreCandidateCommandHandler : IRequestHandler<RestoreCandidateCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreCandidateCommandHandler> _logger;

        public RestoreCandidateCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<RestoreCandidateCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreCandidateCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.Id} not found.");

            candidate.IsDeleted = false;
            candidate.DeletedAtUtc = null;
            candidate.DeletedBy = null;
            candidate.UpdatedAtUtc = DateTime.UtcNow;
            candidate.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(candidate, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored candidate {CandidateId}", candidate.Id);

            await _auditLogger.LogAsync("Candidate", candidate.Id, "Restored", ct: cancellationToken);
        }
    }
}

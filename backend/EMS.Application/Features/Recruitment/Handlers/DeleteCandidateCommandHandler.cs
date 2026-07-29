using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class DeleteCandidateCommandHandler : IRequestHandler<DeleteCandidateCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteCandidateCommandHandler> _logger;

        public DeleteCandidateCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<DeleteCandidateCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteCandidateCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.Id} not found.");

            candidate.DeletedAtUtc = DateTime.UtcNow;
            candidate.DeletedBy = _currentUser.UserId;

            await _repo.DeleteAsync(candidate, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) candidate {CandidateId}", candidate.Id);

            await _auditLogger.LogAsync("Candidate", candidate.Id, "Deleted", ct: cancellationToken);
        }
    }
}

using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class WithdrawCandidateCommandHandler : IRequestHandler<WithdrawCandidateCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<WithdrawCandidateCommandHandler> _logger;

        public WithdrawCandidateCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<WithdrawCandidateCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(WithdrawCandidateCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.Id} not found.");

            if (candidate.Status is CandidateStatus.Hired or CandidateStatus.Rejected or CandidateStatus.Withdrawn)
                throw new InvalidOperationException($"Candidate {candidate.CandidateNumber} is already in a terminal state ({candidate.Status}).");

            candidate.Status = CandidateStatus.Withdrawn;
            candidate.Notes = string.IsNullOrWhiteSpace(request.Reason) ? candidate.Notes : $"{candidate.Notes}\nWithdrawn: {request.Reason}".Trim();
            candidate.UpdatedAtUtc = DateTime.UtcNow;
            candidate.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(candidate, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Withdrew candidate {CandidateId}", candidate.Id);

            await _auditLogger.LogAsync("Candidate", candidate.Id, "Withdrawn", ct: cancellationToken);
        }
    }
}

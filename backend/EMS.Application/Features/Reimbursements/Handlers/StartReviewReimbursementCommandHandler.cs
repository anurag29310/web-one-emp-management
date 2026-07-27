using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class StartReviewReimbursementCommandHandler : IRequestHandler<StartReviewReimbursementCommand>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<StartReviewReimbursementCommandHandler> _logger;

        public StartReviewReimbursementCommandHandler(IReimbursementRepository repo, IAuditLogger auditLogger, ILogger<StartReviewReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(StartReviewReimbursementCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.Id} not found.");

            if (reimbursement.Status != ReimbursementStatus.Submitted)
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} must be Submitted to start review (currently {reimbursement.Status}).");

            reimbursement.Status = ReimbursementStatus.UnderReview;
            reimbursement.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Started review of reimbursement {ReimbursementId}", reimbursement.Id);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "ReviewStarted", ct: cancellationToken);
        }
    }
}

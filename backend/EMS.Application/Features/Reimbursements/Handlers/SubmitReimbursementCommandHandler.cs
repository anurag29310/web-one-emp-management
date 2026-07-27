using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class SubmitReimbursementCommandHandler : IRequestHandler<SubmitReimbursementCommand>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SubmitReimbursementCommandHandler> _logger;

        public SubmitReimbursementCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<SubmitReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(SubmitReimbursementCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.Id} not found.");

            var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                throw new UnauthorizedAccessException("You can only submit your own reimbursements.");

            if (reimbursement.Status is not (ReimbursementStatus.Draft or ReimbursementStatus.ChangesRequested))
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} must be Draft or ChangesRequested to submit (currently {reimbursement.Status}).");

            reimbursement.Status = ReimbursementStatus.Submitted;
            reimbursement.SubmittedAtUtc = DateTime.UtcNow;
            reimbursement.UpdatedAtUtc = DateTime.UtcNow;
            reimbursement.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Submitted reimbursement {ReimbursementId}", reimbursement.Id);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "Submitted", ct: cancellationToken);
        }
    }
}

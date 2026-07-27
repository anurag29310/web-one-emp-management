using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class ApproveReimbursementCommandHandler : IRequestHandler<ApproveReimbursementCommand>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ApproveReimbursementCommandHandler> _logger;

        public ApproveReimbursementCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<ApproveReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ApproveReimbursementCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.Id} not found.");

            // "Employee cannot approve own reimbursement" — checked regardless of role, since an
            // Admin caller may also have their own employee record and their own claims.
            var approver = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (approver?.EmployeeId != null && approver.EmployeeId == reimbursement.EmployeeId)
                throw new InvalidOperationException("You cannot approve your own reimbursement.");

            if (reimbursement.Status != ReimbursementStatus.UnderReview)
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} must be UnderReview to approve (currently {reimbursement.Status}).");

            reimbursement.Status = ReimbursementStatus.Approved;
            reimbursement.ApprovedAtUtc = DateTime.UtcNow;
            reimbursement.ApprovedBy = request.RequestingUserId;
            reimbursement.UpdatedAtUtc = DateTime.UtcNow;
            reimbursement.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Approved reimbursement {ReimbursementId}", reimbursement.Id);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "Approved", ct: cancellationToken);
        }
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class RejectReimbursementCommandHandler : IRequestHandler<RejectReimbursementCommand>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RejectReimbursementCommandHandler> _logger;

        public RejectReimbursementCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<RejectReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RejectReimbursementCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.Id} not found.");

            var reviewer = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (reviewer?.EmployeeId != null && reviewer.EmployeeId == reimbursement.EmployeeId)
                throw new InvalidOperationException("You cannot reject your own reimbursement.");

            if (reimbursement.Status != ReimbursementStatus.UnderReview)
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} must be UnderReview to reject (currently {reimbursement.Status}).");

            reimbursement.Status = ReimbursementStatus.Rejected;
            reimbursement.ReviewRemarks = request.Remarks;
            reimbursement.UpdatedAtUtc = DateTime.UtcNow;
            reimbursement.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Rejected reimbursement {ReimbursementId}", reimbursement.Id);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "Rejected", newValues: new { request.Remarks }, ct: cancellationToken);
        }
    }
}

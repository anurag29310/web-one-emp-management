using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class DeleteReimbursementCommandHandler : IRequestHandler<DeleteReimbursementCommand>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteReimbursementCommandHandler> _logger;

        public DeleteReimbursementCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<DeleteReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteReimbursementCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.Id} not found.");

            var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                throw new UnauthorizedAccessException("You can only delete your own reimbursements.");

            if (reimbursement.Status != ReimbursementStatus.Draft)
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} can only be deleted while Draft (currently {reimbursement.Status}).");

            reimbursement.IsDeleted = true;
            reimbursement.DeletedAtUtc = DateTime.UtcNow;
            reimbursement.DeletedBy = request.RequestingUserId;

            await _repo.UpdateAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) reimbursement {ReimbursementId}", reimbursement.Id);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "Deleted", ct: cancellationToken);
        }
    }
}

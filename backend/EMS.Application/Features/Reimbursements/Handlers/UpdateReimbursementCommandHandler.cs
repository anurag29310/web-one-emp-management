using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class UpdateReimbursementCommandHandler : IRequestHandler<UpdateReimbursementCommand, Reimbursement>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateReimbursementCommandHandler> _logger;

        public UpdateReimbursementCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<UpdateReimbursementCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Reimbursement> Handle(UpdateReimbursementCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.Id} not found.");

            var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                throw new UnauthorizedAccessException("You can only edit your own reimbursements.");

            if (reimbursement.Status is not (ReimbursementStatus.Draft or ReimbursementStatus.ChangesRequested))
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} can only be edited while Draft or ChangesRequested (currently {reimbursement.Status}).");

            reimbursement.ExpenseTitle = request.ExpenseTitle;
            reimbursement.ExpenseCategory = request.ExpenseCategory;
            reimbursement.ExpenseDate = request.ExpenseDate;
            reimbursement.Amount = request.Amount;
            reimbursement.Currency = request.Currency;
            reimbursement.Description = request.Description;
            reimbursement.Notes = request.Notes;
            reimbursement.UpdatedAtUtc = DateTime.UtcNow;
            reimbursement.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateAsync(reimbursement, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated reimbursement {ReimbursementId}", reimbursement.Id);

            await _auditLogger.LogAsync("Reimbursement", reimbursement.Id, "Updated", ct: cancellationToken);

            return reimbursement;
        }
    }
}

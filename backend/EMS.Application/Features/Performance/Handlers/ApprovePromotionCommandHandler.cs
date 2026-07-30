using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    /// <summary>Approving applies the Employee's designation/department change immediately if
    /// EffectiveDate has already arrived; otherwise the change is deferred and applied later by the
    /// daily sweep (RunDailySweepCommand) once EffectiveDate arrives — see PromotionApplier.</summary>
    public class ApprovePromotionCommandHandler : IRequestHandler<ApprovePromotionCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ApprovePromotionCommandHandler> _logger;

        public ApprovePromotionCommandHandler(IPerformanceRepository repo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<ApprovePromotionCommandHandler> logger)
        {
            _repo = repo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ApprovePromotionCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _repo.GetPromotionByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Promotion {request.Id} not found.");

            if (promotion.Status != PromotionStatus.Proposed)
                throw new InvalidOperationException($"Promotion {promotion.PromotionNumber} must be Proposed to approve (currently {promotion.Status}).");

            var now = DateTime.UtcNow;

            promotion.Status = PromotionStatus.Approved;
            promotion.DecidedByUserId = request.RequestingUserId;
            promotion.DecidedAtUtc = now;
            promotion.DecisionNotes = request.DecisionNotes;
            promotion.UpdatedAtUtc = now;
            promotion.UpdatedBy = request.RequestingUserId;

            if (promotion.EffectiveDate <= now)
                await PromotionApplier.ApplyAsync(_employeeRepo, promotion, now, cancellationToken);

            await _repo.UpdatePromotionAsync(promotion, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            if (promotion.AppliedAtUtc.HasValue)
                _logger.LogInformation("Approved promotion {PromotionId}, employee {EmployeeId} moved to designation {ToDesignationId}", promotion.Id, promotion.EmployeeId, promotion.ToDesignationId);
            else
                _logger.LogInformation("Approved promotion {PromotionId} for employee {EmployeeId}; deferred until EffectiveDate {EffectiveDate}", promotion.Id, promotion.EmployeeId, promotion.EffectiveDate);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Approved", newValues: new { promotion.ToDesignationId, promotion.ToDepartmentId, Deferred = !promotion.AppliedAtUtc.HasValue }, ct: cancellationToken);
        }
    }
}

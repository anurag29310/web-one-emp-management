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
    /// <summary>Approving takes effect immediately — the Employee's designation/department is updated
    /// on approval, not on EffectiveDate. There's no background job to apply it later, matching
    /// Offer.Expired's precedent of a field that's defined but not automated.</summary>
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

            var employee = await _employeeRepo.GetByIdAsync(promotion.EmployeeId, cancellationToken)
                ?? throw new InvalidOperationException($"Employee {promotion.EmployeeId} not found.");

            employee.DesignationId = promotion.ToDesignationId;
            if (promotion.ToDepartmentId.HasValue)
                employee.DepartmentId = promotion.ToDepartmentId;
            employee.UpdatedAtUtc = DateTime.UtcNow;
            await _employeeRepo.UpdateAsync(employee, cancellationToken);

            promotion.Status = PromotionStatus.Approved;
            promotion.DecidedByUserId = request.RequestingUserId;
            promotion.DecidedAtUtc = DateTime.UtcNow;
            promotion.DecisionNotes = request.DecisionNotes;
            promotion.UpdatedAtUtc = DateTime.UtcNow;
            promotion.UpdatedBy = request.RequestingUserId;
            await _repo.UpdatePromotionAsync(promotion, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Approved promotion {PromotionId}, employee {EmployeeId} moved to designation {ToDesignationId}", promotion.Id, promotion.EmployeeId, promotion.ToDesignationId);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Approved", newValues: new { promotion.ToDesignationId, promotion.ToDepartmentId }, ct: cancellationToken);
        }
    }
}

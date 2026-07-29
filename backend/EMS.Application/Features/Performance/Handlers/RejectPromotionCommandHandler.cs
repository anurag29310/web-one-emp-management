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
    public class RejectPromotionCommandHandler : IRequestHandler<RejectPromotionCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RejectPromotionCommandHandler> _logger;

        public RejectPromotionCommandHandler(IPerformanceRepository repo, IAuditLogger auditLogger, ILogger<RejectPromotionCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RejectPromotionCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _repo.GetPromotionByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Promotion {request.Id} not found.");

            if (promotion.Status != PromotionStatus.Proposed)
                throw new InvalidOperationException($"Promotion {promotion.PromotionNumber} must be Proposed to reject (currently {promotion.Status}).");

            promotion.Status = PromotionStatus.Rejected;
            promotion.DecidedByUserId = request.RequestingUserId;
            promotion.DecidedAtUtc = DateTime.UtcNow;
            promotion.DecisionNotes = request.DecisionNotes;
            promotion.UpdatedAtUtc = DateTime.UtcNow;
            promotion.UpdatedBy = request.RequestingUserId;

            await _repo.UpdatePromotionAsync(promotion, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Rejected promotion {PromotionId}", promotion.Id);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Rejected", ct: cancellationToken);
        }
    }
}

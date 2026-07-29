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
    public class WithdrawPromotionCommandHandler : IRequestHandler<WithdrawPromotionCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<WithdrawPromotionCommandHandler> _logger;

        public WithdrawPromotionCommandHandler(IPerformanceRepository repo, IAuditLogger auditLogger, ILogger<WithdrawPromotionCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(WithdrawPromotionCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _repo.GetPromotionByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Promotion {request.Id} not found.");

            if (promotion.Status != PromotionStatus.Proposed)
                throw new InvalidOperationException($"Promotion {promotion.PromotionNumber} must be Proposed to withdraw (currently {promotion.Status}).");

            if (!request.IsPrivileged && promotion.ProposedByUserId != request.RequestingUserId)
                throw new UnauthorizedAccessException("Only the user who proposed this promotion can withdraw it.");

            promotion.Status = PromotionStatus.Withdrawn;
            promotion.UpdatedAtUtc = DateTime.UtcNow;
            promotion.UpdatedBy = request.RequestingUserId;

            await _repo.UpdatePromotionAsync(promotion, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Withdrew promotion {PromotionId}", promotion.Id);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Withdrawn", ct: cancellationToken);
        }
    }
}

using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class RestorePromotionCommandHandler : IRequestHandler<RestorePromotionCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestorePromotionCommandHandler> _logger;

        public RestorePromotionCommandHandler(IPerformanceRepository repo, IAuditLogger auditLogger, ILogger<RestorePromotionCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestorePromotionCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _repo.GetPromotionByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Promotion {request.Id} not found.");

            if (!promotion.IsDeleted)
                throw new InvalidOperationException($"Promotion {promotion.PromotionNumber} is not deleted.");

            promotion.DeletedAtUtc = null;
            promotion.DeletedBy = null;
            promotion.UpdatedAtUtc = DateTime.UtcNow;
            promotion.UpdatedBy = request.RequestingUserId;

            await _repo.RestorePromotionAsync(promotion, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored performance promotion {PromotionId}", promotion.Id);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Restored", ct: cancellationToken);
        }
    }
}

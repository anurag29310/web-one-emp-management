using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class DeletePromotionCommandHandler : IRequestHandler<DeletePromotionCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeletePromotionCommandHandler> _logger;

        public DeletePromotionCommandHandler(IPerformanceRepository repo, IAuditLogger auditLogger, ILogger<DeletePromotionCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeletePromotionCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _repo.GetPromotionByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Promotion {request.Id} not found.");

            promotion.DeletedAtUtc = DateTime.UtcNow;
            promotion.DeletedBy = request.RequestingUserId;
            await _repo.DeletePromotionAsync(promotion, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted performance promotion {PromotionId}", promotion.Id);

            await _auditLogger.LogAsync("Promotion", promotion.Id, "Deleted", ct: cancellationToken);
        }
    }
}

using EMS.Application.Features.Assets.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class DeleteAssetCommandHandler : IRequestHandler<DeleteAssetCommand>
    {
        private readonly IAssetRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteAssetCommandHandler> _logger;

        public DeleteAssetCommandHandler(IAssetRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<DeleteAssetCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Asset {request.Id} not found.");

            if (asset.Status == AssetStatus.Assigned)
                throw new InvalidOperationException($"Asset {asset.AssetTag} is currently assigned and must be returned before it can be deleted.");

            asset.DeletedAtUtc = DateTime.UtcNow;
            asset.DeletedBy = _currentUser.UserId;

            await _repo.DeleteAsync(asset, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) asset {AssetId}", asset.Id);

            await _auditLogger.LogAsync("Asset", asset.Id, "Deleted", ct: cancellationToken);
        }
    }
}

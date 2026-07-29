using EMS.Application.Features.Assets.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class RestoreAssetCommandHandler : IRequestHandler<RestoreAssetCommand>
    {
        private readonly IAssetRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreAssetCommandHandler> _logger;

        public RestoreAssetCommandHandler(IAssetRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<RestoreAssetCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Asset {request.Id} not found.");

            asset.IsDeleted = false;
            asset.DeletedAtUtc = null;
            asset.DeletedBy = null;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            asset.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(asset, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored asset {AssetId}", asset.Id);

            await _auditLogger.LogAsync("Asset", asset.Id, "Restored", ct: cancellationToken);
        }
    }
}

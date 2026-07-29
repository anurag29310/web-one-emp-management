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
    public class UpdateAssetStatusCommandHandler : IRequestHandler<UpdateAssetStatusCommand>
    {
        private readonly IAssetRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateAssetStatusCommandHandler> _logger;

        public UpdateAssetStatusCommandHandler(IAssetRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<UpdateAssetStatusCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateAssetStatusCommand request, CancellationToken cancellationToken)
        {
            var asset = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Asset {request.Id} not found.");

            if (request.Status == AssetStatus.Assigned)
                throw new InvalidOperationException("Status cannot be set to Assigned directly — use the Assign action.");

            if (asset.Status == AssetStatus.Assigned)
                throw new InvalidOperationException($"Asset {asset.AssetTag} is currently assigned and must be returned before its status can change.");

            asset.Status = request.Status;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            asset.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(asset, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Asset {AssetId} status changed to {Status}", asset.Id, asset.Status);

            await _auditLogger.LogAsync("Asset", asset.Id, "StatusChanged", newValues: new { asset.Status }, ct: cancellationToken);
        }
    }
}

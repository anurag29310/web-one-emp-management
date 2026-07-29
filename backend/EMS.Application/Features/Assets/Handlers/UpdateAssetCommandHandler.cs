using EMS.Application.Features.Assets.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class UpdateAssetCommandHandler : IRequestHandler<UpdateAssetCommand>
    {
        private readonly IAssetRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateAssetCommandHandler> _logger;

        public UpdateAssetCommandHandler(IAssetRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<UpdateAssetCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Asset {request.Id} not found.");

            asset.Category = request.Category;
            asset.Brand = request.Brand;
            asset.Model = request.Model;
            asset.SerialNumber = request.SerialNumber;
            asset.PurchaseDate = request.PurchaseDate;
            asset.PurchaseCost = request.PurchaseCost;
            asset.Notes = request.Notes;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            asset.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(asset, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated asset {AssetId}", asset.Id);

            await _auditLogger.LogAsync("Asset", asset.Id, "Updated", ct: cancellationToken);
        }
    }
}

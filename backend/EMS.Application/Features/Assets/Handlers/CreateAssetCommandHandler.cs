using EMS.Application.Features.Assets.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Assets.Handlers
{
    public class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, Guid>
    {
        private readonly IAssetRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateAssetCommandHandler> _logger;

        public CreateAssetCommandHandler(IAssetRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<CreateAssetCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            var asset = new Asset
            {
                Id = id,
                AssetTag = $"AST-{id:N}".Substring(0, 12).ToUpperInvariant(),
                Category = request.Category,
                Brand = request.Brand,
                Model = request.Model,
                SerialNumber = request.SerialNumber,
                PurchaseDate = request.PurchaseDate,
                PurchaseCost = request.PurchaseCost,
                Notes = request.Notes,
                Status = AssetStatus.Available,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            await _repo.AddAsync(asset, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created asset {AssetTag} ({AssetId})", asset.AssetTag, asset.Id);

            await _auditLogger.LogAsync("Asset", asset.Id, "Created", ct: cancellationToken);

            return asset.Id;
        }
    }
}

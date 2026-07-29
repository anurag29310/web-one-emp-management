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
    public class AssignAssetCommandHandler : IRequestHandler<AssignAssetCommand, Guid>
    {
        private readonly IAssetRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<AssignAssetCommandHandler> _logger;

        public AssignAssetCommandHandler(IAssetRepository repo, IAuditLogger auditLogger, ILogger<AssignAssetCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
        {
            var asset = await _repo.GetByIdAsync(request.AssetId, cancellationToken)
                ?? throw new InvalidOperationException($"Asset {request.AssetId} not found.");

            if (asset.Status != AssetStatus.Available)
                throw new InvalidOperationException($"Asset {asset.AssetTag} is {asset.Status} and cannot be assigned — only an Available asset can be assigned.");

            var now = DateTime.UtcNow;
            var assignment = new AssetAssignment
            {
                Id = Guid.NewGuid(),
                AssetId = request.AssetId,
                EmployeeId = request.EmployeeId,
                AssignedByUserId = request.RequestingUserId,
                AssignedDate = now,
                ExpectedReturnDate = request.ExpectedReturnDate,
                ConditionAtAssignment = request.ConditionAtAssignment,
                Notes = request.Notes,
                CreatedAtUtc = now
            };

            await _repo.AddAssignmentAsync(assignment, cancellationToken);

            asset.Status = AssetStatus.Assigned;
            asset.UpdatedAtUtc = now;
            asset.UpdatedBy = request.RequestingUserId;
            await _repo.UpdateAsync(asset, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Assigned asset {AssetId} to employee {EmployeeId}", asset.Id, request.EmployeeId);

            await _auditLogger.LogAsync("Asset", asset.Id, "Assigned", newValues: new { request.EmployeeId }, ct: cancellationToken);

            return assignment.Id;
        }
    }
}

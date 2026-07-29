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
    public class ReturnAssetCommandHandler : IRequestHandler<ReturnAssetCommand>
    {
        private readonly IAssetRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ReturnAssetCommandHandler> _logger;

        public ReturnAssetCommandHandler(IAssetRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<ReturnAssetCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ReturnAssetCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _repo.GetAssignmentByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Asset assignment {request.Id} not found.");

            if (assignment.ReturnedDate != null)
                throw new InvalidOperationException("This assignment has already been returned.");

            if (request.ResultingAssetStatus == AssetStatus.Assigned)
                throw new InvalidOperationException("ResultingAssetStatus cannot be Assigned — return a specific condition (Available, UnderRepair, Retired, or Lost).");

            var now = DateTime.UtcNow;
            assignment.ReturnedDate = now;
            assignment.ConditionAtReturn = request.ConditionAtReturn;
            assignment.Notes = string.IsNullOrWhiteSpace(request.Notes) ? assignment.Notes : request.Notes;
            assignment.UpdatedAtUtc = now;
            await _repo.UpdateAssignmentAsync(assignment, cancellationToken);

            var asset = await _repo.GetByIdAsync(assignment.AssetId, cancellationToken)
                ?? throw new InvalidOperationException($"Asset {assignment.AssetId} not found.");
            asset.Status = request.ResultingAssetStatus;
            asset.UpdatedAtUtc = now;
            asset.UpdatedBy = _currentUser.UserId;
            await _repo.UpdateAsync(asset, cancellationToken);

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Returned asset {AssetId} (assignment {AssignmentId}); resulting status {Status}", asset.Id, assignment.Id, asset.Status);

            await _auditLogger.LogAsync("Asset", asset.Id, "Returned", newValues: new { assignment.EmployeeId, ResultingStatus = asset.Status }, ct: cancellationToken);
        }
    }
}

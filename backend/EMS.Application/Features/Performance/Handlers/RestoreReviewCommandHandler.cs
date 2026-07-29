using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class RestoreReviewCommandHandler : IRequestHandler<RestoreReviewCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreReviewCommandHandler> _logger;

        public RestoreReviewCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<RestoreReviewCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _repo.GetReviewByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Review {request.Id} not found.");

            if (!review.IsDeleted)
                throw new InvalidOperationException($"Review {review.ReviewNumber} is not deleted.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null || requesterEmployeeId.Value != review.ReviewerEmployeeId)
                    throw new UnauthorizedAccessException("Only the assigned reviewer can restore this review.");
            }

            review.DeletedAtUtc = null;
            review.DeletedBy = null;
            review.UpdatedAtUtc = DateTime.UtcNow;
            review.UpdatedBy = request.RequestingUserId;

            await _repo.RestoreReviewAsync(review, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored performance review {ReviewId}", review.Id);

            await _auditLogger.LogAsync("PerformanceReview", review.Id, "Restored", ct: cancellationToken);
        }
    }
}

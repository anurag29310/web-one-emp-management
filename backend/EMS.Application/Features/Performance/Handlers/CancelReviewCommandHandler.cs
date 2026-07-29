using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class CancelReviewCommandHandler : IRequestHandler<CancelReviewCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CancelReviewCommandHandler> _logger;

        public CancelReviewCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<CancelReviewCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(CancelReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _repo.GetReviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Review {request.Id} not found.");

            if (review.Status is ReviewStatus.Completed or ReviewStatus.Cancelled)
                throw new InvalidOperationException($"Review {review.ReviewNumber} is already {review.Status} and cannot be cancelled.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null || requesterEmployeeId.Value != review.ReviewerEmployeeId)
                    throw new UnauthorizedAccessException("Only the assigned reviewer can cancel this review.");
            }

            review.Status = ReviewStatus.Cancelled;
            review.Notes = request.Reason != null ? $"{review.Notes} {request.Reason}".Trim() : review.Notes;
            review.UpdatedAtUtc = DateTime.UtcNow;
            review.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateReviewAsync(review, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cancelled performance review {ReviewId}", review.Id);

            await _auditLogger.LogAsync("PerformanceReview", review.Id, "Cancelled", ct: cancellationToken);
        }
    }
}

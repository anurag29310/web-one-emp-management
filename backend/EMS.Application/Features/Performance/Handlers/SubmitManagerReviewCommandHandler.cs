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
    public class SubmitManagerReviewCommandHandler : IRequestHandler<SubmitManagerReviewCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SubmitManagerReviewCommandHandler> _logger;

        public SubmitManagerReviewCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<SubmitManagerReviewCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(SubmitManagerReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _repo.GetReviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Review {request.Id} not found.");

            if (review.Status is not (ReviewStatus.Draft or ReviewStatus.SelfAssessmentSubmitted))
                throw new InvalidOperationException($"Review {review.ReviewNumber} must be Draft or SelfAssessmentSubmitted to submit the manager review (currently {review.Status}).");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null || requesterEmployeeId.Value != review.ReviewerEmployeeId)
                    throw new UnauthorizedAccessException("Only the assigned reviewer can submit the manager review.");
            }

            review.ManagerAssessment = request.ManagerAssessment;
            review.OverallRating = request.OverallRating;
            review.Status = ReviewStatus.Completed;
            review.CompletedAtUtc = DateTime.UtcNow;
            review.UpdatedAtUtc = DateTime.UtcNow;
            review.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateReviewAsync(review, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Manager review submitted for review {ReviewId}, rating {Rating}", review.Id, review.OverallRating);

            await _auditLogger.LogAsync("PerformanceReview", review.Id, "ManagerReviewSubmitted", newValues: new { review.OverallRating }, ct: cancellationToken);
        }
    }
}

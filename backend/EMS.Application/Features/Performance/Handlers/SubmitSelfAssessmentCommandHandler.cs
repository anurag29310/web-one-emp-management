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
    public class SubmitSelfAssessmentCommandHandler : IRequestHandler<SubmitSelfAssessmentCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SubmitSelfAssessmentCommandHandler> _logger;

        public SubmitSelfAssessmentCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<SubmitSelfAssessmentCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(SubmitSelfAssessmentCommand request, CancellationToken cancellationToken)
        {
            var review = await _repo.GetReviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Review {request.Id} not found.");

            if (review.Status != ReviewStatus.Draft)
                throw new InvalidOperationException($"Review {review.ReviewNumber} must be Draft to submit a self-assessment (currently {review.Status}).");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null || requesterEmployeeId.Value != review.EmployeeId)
                    throw new UnauthorizedAccessException("Only the reviewed employee can submit their self-assessment.");
            }

            review.SelfAssessment = request.SelfAssessment;
            review.SelfSubmittedAtUtc = DateTime.UtcNow;
            review.Status = ReviewStatus.SelfAssessmentSubmitted;
            review.UpdatedAtUtc = DateTime.UtcNow;
            review.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateReviewAsync(review, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Self-assessment submitted for review {ReviewId}", review.Id);

            await _auditLogger.LogAsync("PerformanceReview", review.Id, "SelfAssessmentSubmitted", ct: cancellationToken);
        }
    }
}

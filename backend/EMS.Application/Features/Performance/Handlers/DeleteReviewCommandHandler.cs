using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteReviewCommandHandler> _logger;

        public DeleteReviewCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<DeleteReviewCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        {
            var review = await _repo.GetReviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Review {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (requesterEmployeeId == null || requesterEmployeeId.Value != review.ReviewerEmployeeId)
                    throw new UnauthorizedAccessException("Only the assigned reviewer can delete this review.");
            }

            review.DeletedAtUtc = DateTime.UtcNow;
            review.DeletedBy = request.RequestingUserId;
            await _repo.DeleteReviewAsync(review, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted performance review {ReviewId}", review.Id);

            await _auditLogger.LogAsync("PerformanceReview", review.Id, "Deleted", ct: cancellationToken);
        }
    }
}

using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Performance.Handlers
{
    public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Guid>
    {
        private readonly IPerformanceRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateReviewCommandHandler> _logger;

        public CreateReviewCommandHandler(IPerformanceRepository repo, IAuthRepository authRepo, IEmployeeRepository employeeRepo, IAuditLogger auditLogger, ILogger<CreateReviewCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _employeeRepo = employeeRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsPrivileged)
            {
                var requesterEmployeeId = await PerformanceScopeHelper.ResolveRequesterEmployeeIdAsync(_authRepo, request.RequestingUserId, cancellationToken);
                if (!await PerformanceScopeHelper.IsManagerOfAsync(_employeeRepo, requesterEmployeeId, request.EmployeeId, request.IsManager, cancellationToken))
                    throw new UnauthorizedAccessException("You can only create reviews for your own direct reports.");

                // A Manager can only assign themselves as reviewer, not delegate to someone else — only
                // Admin/HR may set an arbitrary reviewer.
                if (requesterEmployeeId != request.ReviewerEmployeeId)
                    throw new UnauthorizedAccessException("You must set yourself as the reviewer.");
            }

            var id = Guid.NewGuid();
            var review = new PerformanceReview
            {
                Id = id,
                ReviewNumber = $"REV-{id:N}".Substring(0, 12).ToUpperInvariant(),
                EmployeeId = request.EmployeeId,
                ReviewerEmployeeId = request.ReviewerEmployeeId,
                ReviewPeriodStart = request.ReviewPeriodStart,
                ReviewPeriodEnd = request.ReviewPeriodEnd,
                Status = ReviewStatus.Draft,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = request.RequestingUserId,
                IsDeleted = false
            };

            await _repo.AddReviewAsync(review, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created performance review {ReviewNumber} ({ReviewId}) for employee {EmployeeId}", review.ReviewNumber, review.Id, review.EmployeeId);

            await _auditLogger.LogAsync("PerformanceReview", review.Id, "Created", ct: cancellationToken);

            return review.Id;
        }
    }
}

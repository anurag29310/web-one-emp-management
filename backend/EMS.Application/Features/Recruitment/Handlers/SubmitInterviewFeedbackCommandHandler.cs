using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class SubmitInterviewFeedbackCommandHandler : IRequestHandler<SubmitInterviewFeedbackCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SubmitInterviewFeedbackCommandHandler> _logger;

        public SubmitInterviewFeedbackCommandHandler(IRecruitmentRepository repo, IAuthRepository authRepo, IAuditLogger auditLogger, ILogger<SubmitInterviewFeedbackCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(SubmitInterviewFeedbackCommand request, CancellationToken cancellationToken)
        {
            var interview = await _repo.GetInterviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Interview {request.Id} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != interview.InterviewerEmployeeId)
                    throw new UnauthorizedAccessException("You can only submit feedback for interviews assigned to you.");
            }

            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Only a Scheduled interview can receive feedback (currently {interview.Status}).");

            interview.Status = InterviewStatus.Completed;
            interview.Feedback = request.Feedback;
            interview.Rating = request.Rating;
            interview.Outcome = request.Outcome;
            interview.UpdatedAtUtc = DateTime.UtcNow;
            interview.UpdatedBy = request.RequestingUserId;

            await _repo.UpdateInterviewAsync(interview, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Recorded feedback for interview {InterviewId}: {Outcome}", interview.Id, interview.Outcome);

            await _auditLogger.LogAsync("Interview", interview.Id, "FeedbackSubmitted", ct: cancellationToken);
        }
    }
}

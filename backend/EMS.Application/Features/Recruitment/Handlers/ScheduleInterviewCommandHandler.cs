using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class ScheduleInterviewCommandHandler : IRequestHandler<ScheduleInterviewCommand, Guid>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ScheduleInterviewCommandHandler> _logger;

        public ScheduleInterviewCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<ScheduleInterviewCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.CandidateId, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.CandidateId} not found.");

            if (candidate.Status is CandidateStatus.Hired or CandidateStatus.Rejected or CandidateStatus.Withdrawn)
                throw new InvalidOperationException($"Candidate {candidate.CandidateNumber} is in a terminal state ({candidate.Status}) and cannot be scheduled for an interview.");

            var now = DateTime.UtcNow;
            var interview = new Interview
            {
                Id = Guid.NewGuid(),
                CandidateId = request.CandidateId,
                InterviewerEmployeeId = request.InterviewerEmployeeId,
                Round = request.Round,
                Mode = request.Mode,
                ScheduledAtUtc = request.ScheduledAtUtc,
                DurationMinutes = request.DurationMinutes,
                Status = InterviewStatus.Scheduled,
                Outcome = InterviewOutcome.Pending,
                CreatedAtUtc = now,
                CreatedBy = _currentUser.UserId
            };

            await _repo.AddInterviewAsync(interview, cancellationToken);

            // First interview scheduled moves the candidate into the Interviewing stage.
            if (candidate.Status is CandidateStatus.Applied or CandidateStatus.Screening)
            {
                candidate.Status = CandidateStatus.Interviewing;
                candidate.UpdatedAtUtc = now;
                candidate.UpdatedBy = _currentUser.UserId;
                await _repo.UpdateAsync(candidate, cancellationToken);
            }

            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Scheduled interview {InterviewId} for candidate {CandidateId}", interview.Id, candidate.Id);

            await _auditLogger.LogAsync("Interview", interview.Id, "Scheduled", ct: cancellationToken);

            return interview.Id;
        }
    }
}

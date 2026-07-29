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
    public class RescheduleInterviewCommandHandler : IRequestHandler<RescheduleInterviewCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RescheduleInterviewCommandHandler> _logger;

        public RescheduleInterviewCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<RescheduleInterviewCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RescheduleInterviewCommand request, CancellationToken cancellationToken)
        {
            var interview = await _repo.GetInterviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Interview {request.Id} not found.");

            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Only a Scheduled interview can be rescheduled (currently {interview.Status}).");

            interview.ScheduledAtUtc = request.ScheduledAtUtc;
            interview.DurationMinutes = request.DurationMinutes;
            interview.UpdatedAtUtc = DateTime.UtcNow;
            interview.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateInterviewAsync(interview, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Rescheduled interview {InterviewId}", interview.Id);

            await _auditLogger.LogAsync("Interview", interview.Id, "Rescheduled", ct: cancellationToken);
        }
    }
}

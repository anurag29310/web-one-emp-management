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
    public class MarkInterviewNoShowCommandHandler : IRequestHandler<MarkInterviewNoShowCommand>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<MarkInterviewNoShowCommandHandler> _logger;

        public MarkInterviewNoShowCommandHandler(IRecruitmentRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<MarkInterviewNoShowCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(MarkInterviewNoShowCommand request, CancellationToken cancellationToken)
        {
            var interview = await _repo.GetInterviewByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Interview {request.Id} not found.");

            if (interview.Status != InterviewStatus.Scheduled)
                throw new InvalidOperationException($"Only a Scheduled interview can be marked NoShow (currently {interview.Status}).");

            interview.Status = InterviewStatus.NoShow;
            interview.UpdatedAtUtc = DateTime.UtcNow;
            interview.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateInterviewAsync(interview, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Marked interview {InterviewId} as NoShow", interview.Id);

            await _auditLogger.LogAsync("Interview", interview.Id, "NoShow", ct: cancellationToken);
        }
    }
}

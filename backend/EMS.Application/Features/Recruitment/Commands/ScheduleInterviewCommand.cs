using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class ScheduleInterviewCommand : IRequest<Guid>
    {
        public Guid CandidateId { get; set; }
        public Guid InterviewerEmployeeId { get; set; }
        public string Round { get; set; } = null!;
        public InterviewMode Mode { get; set; }
        public DateTime ScheduledAtUtc { get; set; }
        public int? DurationMinutes { get; set; }
    }
}

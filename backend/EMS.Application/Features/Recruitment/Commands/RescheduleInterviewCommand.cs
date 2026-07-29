using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class RescheduleInterviewCommand : IRequest
    {
        public Guid Id { get; set; }
        public DateTime ScheduledAtUtc { get; set; }
        public int? DurationMinutes { get; set; }
    }
}

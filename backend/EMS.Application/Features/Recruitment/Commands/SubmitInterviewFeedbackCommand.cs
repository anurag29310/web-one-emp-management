using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    public class SubmitInterviewFeedbackCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Feedback { get; set; } = null!;
        public int Rating { get; set; }
        public InterviewOutcome Outcome { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin/HR role and may submit feedback on behalf of the assigned interviewer.</summary>
        public bool IsPrivileged { get; set; }
    }
}

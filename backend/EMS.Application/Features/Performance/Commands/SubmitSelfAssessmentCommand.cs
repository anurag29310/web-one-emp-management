using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class SubmitSelfAssessmentCommand : IRequest
    {
        public Guid Id { get; set; }
        public string SelfAssessment { get; set; } = null!;

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

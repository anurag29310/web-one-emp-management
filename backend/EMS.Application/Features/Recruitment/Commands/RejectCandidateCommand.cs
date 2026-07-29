using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    /// <summary>Terminal action — the candidate is not being taken forward at any stage of the pipeline.</summary>
    public class RejectCandidateCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }
    }
}

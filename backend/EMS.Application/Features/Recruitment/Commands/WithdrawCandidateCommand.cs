using MediatR;
using System;

namespace EMS.Application.Features.Recruitment.Commands
{
    /// <summary>Terminal action — the candidate themselves withdrew from the process (as opposed to Reject, which is the company's decision).</summary>
    public class WithdrawCandidateCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Companies.Commands
{
    /// <summary>Un-suspends a company (Suspended/Inactive -> Active), or approves a still-pending
    /// registration (PendingApproval -> Trial) — see ApproveCompanyCommand for the latter, which is
    /// the more explicit action for that specific transition. This command is for the general
    /// "make this company able to log in again" case.</summary>
    public class ActivateCompanyCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}

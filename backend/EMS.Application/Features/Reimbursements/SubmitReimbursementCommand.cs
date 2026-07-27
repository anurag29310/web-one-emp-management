using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Draft or ChangesRequested → Submitted. Owner-only, no Admin override — matches "Submit reimbursement" as an Employee-only action.</summary>
    public class SubmitReimbursementCommand : IRequest
    {
        public Guid Id { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Admin-only (CanManageReimbursements policy). UnderReview → Approved. Blocked if the approver's own employee record is the claimant ("Employee cannot approve own reimbursement").</summary>
    public class ApproveReimbursementCommand : IRequest
    {
        public Guid Id { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

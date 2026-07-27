using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Admin-only (CanManageReimbursements policy). UnderReview → Rejected.</summary>
    public class RejectReimbursementCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Remarks { get; set; } = null!;

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Admin-only (CanManageReimbursements policy). Submitted → UnderReview.</summary>
    public class StartReviewReimbursementCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}

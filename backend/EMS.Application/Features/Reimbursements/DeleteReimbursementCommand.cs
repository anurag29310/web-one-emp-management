using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    /// <summary>Draft only — "Delete Draft reimbursement". Owner-only, no Admin override.</summary>
    public class DeleteReimbursementCommand : IRequest
    {
        public Guid Id { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

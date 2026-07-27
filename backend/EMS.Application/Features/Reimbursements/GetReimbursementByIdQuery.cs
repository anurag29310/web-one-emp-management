using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Reimbursements
{
    public class GetReimbursementByIdQuery : IRequest<Reimbursement?>
    {
        public Guid Id { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may view any reimbursement.</summary>
        public bool IsPrivileged { get; set; }
    }
}

using EMS.Application.Features.Reimbursements.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Reimbursements
{
    public class GetReimbursementAttachmentsQuery : IRequest<IEnumerable<ReimbursementAttachmentDto>>
    {
        public Guid ReimbursementId { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may view attachments on any reimbursement.</summary>
        public bool IsPrivileged { get; set; }
    }
}

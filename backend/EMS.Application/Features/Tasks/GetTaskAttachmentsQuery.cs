using EMS.Application.Features.Tasks.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Tasks
{
    public class GetTaskAttachmentsQuery : IRequest<IEnumerable<TaskAttachmentDto>>
    {
        public Guid TaskId { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may view attachments on any task.</summary>
        public bool IsPrivileged { get; set; }
    }
}

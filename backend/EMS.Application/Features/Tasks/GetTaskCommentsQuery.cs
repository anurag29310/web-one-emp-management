using EMS.Application.Features.Tasks.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Tasks
{
    public class GetTaskCommentsQuery : IRequest<IEnumerable<TaskCommentDto>>
    {
        public Guid TaskId { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may view comments on any task.</summary>
        public bool IsPrivileged { get; set; }
    }
}

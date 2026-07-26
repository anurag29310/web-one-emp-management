using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class AddTaskCommentCommand : IRequest<TaskComment>
    {
        public Guid TaskId { get; set; }
        public string Comment { get; set; } = null!;

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may act on behalf of the assignee.</summary>
        public bool IsPrivileged { get; set; }
    }
}

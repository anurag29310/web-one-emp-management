using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    /// <summary>The "Update progress" employee action — moves an in-flight task between InProgress and OnHold.</summary>
    public class UpdateTaskProgressCommand : IRequest
    {
        public Guid Id { get; set; }
        public TaskItemStatus Status { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may act on behalf of the assignee.</summary>
        public bool IsPrivileged { get; set; }
    }
}

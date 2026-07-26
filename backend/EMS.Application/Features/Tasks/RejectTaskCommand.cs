using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class RejectTaskCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? Reason { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may act on behalf of the assignee.</summary>
        public bool IsPrivileged { get; set; }
    }
}

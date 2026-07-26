using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class GetTaskByIdQuery : IRequest<TaskItem?>
    {
        public Guid Id { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may view any task.</summary>
        public bool IsPrivileged { get; set; }
    }
}

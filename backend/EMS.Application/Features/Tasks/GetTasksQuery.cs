using EMS.Application.Common.DTOs;
using EMS.Application.Features.Tasks.DTOs;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class GetTasksQuery : IRequest<PagedResult<TaskItemDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? AssignedEmployeeId { get; set; }
        public Guid? ClientId { get; set; }
        public TaskItemStatus? Status { get; set; }
        public TaskItemPriority? Priority { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin role and may see every task regardless of AssignedEmployeeId.</summary>
        public bool IsPrivileged { get; set; }
    }
}

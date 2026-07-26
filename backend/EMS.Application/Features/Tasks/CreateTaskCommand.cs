using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class CreateTaskCommand : IRequest<TaskItem>
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid? ClientId { get; set; }
        public Guid AssignedEmployeeId { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity — always the Admin creating the task.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

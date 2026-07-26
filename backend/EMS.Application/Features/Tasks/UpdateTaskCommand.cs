using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class UpdateTaskCommand : IRequest<TaskItem>
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Guid? ClientId { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskItemPriority Priority { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

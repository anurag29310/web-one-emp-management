using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    public class ReassignTaskCommand : IRequest<TaskItem>
    {
        public Guid Id { get; set; }
        public Guid AssignedEmployeeId { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Tasks
{
    /// <summary>Admin-only — gated by the CanManageTasks policy at the controller, not self-scoped.</summary>
    public class CancelTaskCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}

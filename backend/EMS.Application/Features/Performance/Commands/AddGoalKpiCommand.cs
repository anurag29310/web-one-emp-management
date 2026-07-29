using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class AddGoalKpiCommand : IRequest<Guid>
    {
        public Guid GoalId { get; set; }
        public string Name { get; set; } = null!;
        public decimal TargetValue { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

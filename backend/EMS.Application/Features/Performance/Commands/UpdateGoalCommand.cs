using EMS.Domain.Enums;
using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class UpdateGoalCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public DateTime TargetDate { get; set; }
        public decimal? Weight { get; set; }
        public GoalStatus Status { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

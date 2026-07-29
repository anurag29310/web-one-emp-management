using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class CreateGoalCommand : IRequest<Guid>
    {
        public Guid EmployeeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime TargetDate { get; set; }
        public decimal? Weight { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

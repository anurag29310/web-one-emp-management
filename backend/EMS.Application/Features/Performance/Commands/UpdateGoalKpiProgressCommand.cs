using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class UpdateGoalKpiProgressCommand : IRequest
    {
        public Guid Id { get; set; }
        public decimal CurrentValue { get; set; }
        public string? Notes { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class UpdateGoalProgressCommand : IRequest
    {
        public Guid Id { get; set; }
        public int ProgressPercent { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

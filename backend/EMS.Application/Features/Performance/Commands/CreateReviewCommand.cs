using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class CreateReviewCommand : IRequest<Guid>
    {
        public Guid EmployeeId { get; set; }
        public Guid ReviewerEmployeeId { get; set; }
        public DateTime ReviewPeriodStart { get; set; }
        public DateTime ReviewPeriodEnd { get; set; }
        public string? Notes { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

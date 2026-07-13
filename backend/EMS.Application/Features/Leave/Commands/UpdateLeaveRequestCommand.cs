using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class UpdateLeaveRequestCommand : IRequest
    {
        public Guid Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class CreateLeaveRequestCommand : IRequest<EMS.Domain.Entities.LeaveRequest>
    {
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDays { get; set; }
        public string? Reason { get; set; }

        /// <summary>Set by the controller from the caller's identity; not client-settable in practice since it is overwritten after model binding.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin/HR/Manager role and may act on any employee's behalf.</summary>
        public bool IsPrivileged { get; set; }
    }
}

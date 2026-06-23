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
    }
}

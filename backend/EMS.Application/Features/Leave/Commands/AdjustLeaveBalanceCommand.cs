using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class AdjustLeaveBalanceCommand : IRequest
    {
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public int Year { get; set; }
        public decimal Adjusted { get; set; }
        public string? Reason { get; set; }
    }
}

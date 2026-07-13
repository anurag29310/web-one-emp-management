using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class UpdateLeaveTypeCommand : IRequest<EMS.Domain.Entities.LeaveType>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public bool IsPaid { get; set; }
        public bool RequiresApproval { get; set; }
        public decimal? AnnualEntitlementDays { get; set; }
    }
}

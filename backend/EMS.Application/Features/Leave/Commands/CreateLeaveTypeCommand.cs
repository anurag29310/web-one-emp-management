using MediatR;

namespace EMS.Application.Features.Leave.Commands
{
    public class CreateLeaveTypeCommand : IRequest<EMS.Domain.Entities.LeaveType>
    {
        public string Name { get; set; } = null!;
        public string? Code { get; set; }
        public bool IsPaid { get; set; }
        public bool RequiresApproval { get; set; }
        public decimal? AnnualEntitlementDays { get; set; }
    }
}

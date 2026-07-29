using MediatR;
using System;

namespace EMS.Application.Features.Performance.Commands
{
    public class ProposePromotionCommand : IRequest<Guid>
    {
        public Guid EmployeeId { get; set; }
        public Guid ToDesignationId { get; set; }
        public Guid? ToDepartmentId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; } = null!;

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsManager { get; set; }
    }
}

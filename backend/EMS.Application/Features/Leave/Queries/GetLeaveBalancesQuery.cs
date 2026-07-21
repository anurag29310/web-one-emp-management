using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeaveBalancesQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.LeaveBalance>>
    {
        public Guid EmployeeId { get; set; }
        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

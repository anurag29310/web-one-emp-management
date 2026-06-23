using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeaveBalancesQuery : IRequest<System.Collections.Generic.IEnumerable<EMS.Domain.Entities.LeaveBalance>>
    {
        public Guid EmployeeId { get; set; }
    }
}

using MediatR;
using System.Collections.Generic;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeaveTypesQuery : IRequest<IEnumerable<EMS.Domain.Entities.LeaveType>>
    {
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeaveTypeByIdQuery : IRequest<EMS.Domain.Entities.LeaveType?>
    {
        public Guid Id { get; set; }
    }
}

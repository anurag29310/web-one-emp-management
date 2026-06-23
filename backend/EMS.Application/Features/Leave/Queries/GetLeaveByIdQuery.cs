using MediatR;
using System;

namespace EMS.Application.Features.Leave.Queries
{
    public class GetLeaveByIdQuery : IRequest<EMS.Domain.Entities.LeaveRequest?>
    {
        public Guid Id { get; set; }
    }
}

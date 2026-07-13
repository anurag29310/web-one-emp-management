using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class CancelLeaveRequestCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid RequestedByUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

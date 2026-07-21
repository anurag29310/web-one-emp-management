using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class RestoreLeaveTypeCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}

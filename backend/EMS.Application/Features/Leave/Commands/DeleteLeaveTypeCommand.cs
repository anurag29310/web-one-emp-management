using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class DeleteLeaveTypeCommand : IRequest
    {
        public Guid Id { get; set; }
    }
}

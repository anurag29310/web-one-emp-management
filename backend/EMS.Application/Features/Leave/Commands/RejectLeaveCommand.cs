
using MediatR;
using System;

namespace EMS.Application.Features.Leave.Commands
{
    public class RejectLeaveCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid ApproverId { get; set; }
        public string? Comments { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Commands
{
    public class RestoreConversationCommand : IRequest
    {
        public Guid Id { get; set; }
        public Guid RequestingUserId { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Commands
{
    public class MarkConversationReadCommand : IRequest
    {
        public Guid ConversationId { get; set; }
        public Guid RequestingUserId { get; set; }
    }
}

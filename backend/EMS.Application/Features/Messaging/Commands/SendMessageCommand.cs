using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Commands
{
    public class SendMessageCommand : IRequest<Guid>
    {
        public Guid ConversationId { get; set; }
        public string Body { get; set; } = null!;

        public Guid RequestingUserId { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Commands
{
    public class LeaveConversationCommand : IRequest
    {
        public Guid ConversationId { get; set; }
        public Guid RequestingUserId { get; set; }
    }
}

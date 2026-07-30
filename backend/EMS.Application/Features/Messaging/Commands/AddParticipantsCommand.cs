using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Messaging.Commands
{
    public class AddParticipantsCommand : IRequest
    {
        public Guid ConversationId { get; set; }
        public List<Guid> UserIds { get; set; } = new();

        public Guid RequestingUserId { get; set; }
    }
}

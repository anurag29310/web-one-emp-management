using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Messaging.Commands
{
    public class CreateConversationCommand : IRequest<Guid>
    {
        public List<Guid> ParticipantUserIds { get; set; } = new();
        public string? Title { get; set; }
        public string InitialMessageBody { get; set; } = null!;

        public Guid RequestingUserId { get; set; }
    }
}

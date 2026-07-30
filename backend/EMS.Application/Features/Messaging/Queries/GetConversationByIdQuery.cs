using EMS.Application.Features.Messaging.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Queries
{
    public class GetConversationByIdQuery : IRequest<ConversationDto?>
    {
        public Guid Id { get; set; }
        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Queries
{
    /// <summary>Number of conversations with at least one unread message for the requesting user — for a nav badge, matching Notifications' onlyUnread convention at the conversation level rather than the message level.</summary>
    public class GetUnreadConversationCountQuery : IRequest<int>
    {
        public Guid RequestingUserId { get; set; }
    }
}

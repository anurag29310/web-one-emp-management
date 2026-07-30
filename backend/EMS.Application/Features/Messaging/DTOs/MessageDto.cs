using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Messaging.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderUserId { get; set; }
        public string? SenderName { get; set; }
        public string Body { get; set; } = null!;
        public DateTime SentAtUtc { get; set; }

        public static MessageDto FromEntity(Message m, string? senderName) => new()
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderUserId = m.SenderUserId,
            SenderName = senderName,
            Body = m.Body,
            SentAtUtc = m.SentAtUtc
        };
    }
}

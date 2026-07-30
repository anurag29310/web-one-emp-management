using System;

namespace EMS.Domain.Entities
{
    /// <summary>Append-only chat message — never updated or deleted, matching the TaskComments/AnnouncementReads convention.</summary>
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderUserId { get; set; }
        public string Body { get; set; } = null!;
        public DateTime SentAtUtc { get; set; }
    }
}

using System;

namespace EMS.Domain.Entities
{
    /// <summary>
    /// Membership + read-cursor row for a Conversation. LastReadAtUtc is a watermark (not a
    /// per-message read receipt table) — unread count is "messages sent after LastReadAtUtc",
    /// matching AnnouncementReads' lightweight-read-tracking precedent but scoped per conversation
    /// instead of per broadcast. LeftAtUtc marks a participant inactive without removing history.
    /// </summary>
    public class MessageParticipant
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAtUtc { get; set; }
        public DateTime? LastReadAtUtc { get; set; }
        public DateTime? LeftAtUtc { get; set; }
    }
}

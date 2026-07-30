using EMS.Domain.Entities;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Messaging.DTOs
{
    public class ConversationDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public bool IsGroup { get; set; }
        public List<MessageParticipantDto> Participants { get; set; } = new();
        public DateTime? LastMessageAtUtc { get; set; }
        public string? LastMessagePreview { get; set; }
        public int UnreadCount { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static ConversationDto FromEntity(Conversation c, List<MessageParticipantDto> participants, string? lastMessagePreview, int unreadCount) => new()
        {
            Id = c.Id,
            Title = c.Title,
            IsGroup = c.IsGroup,
            Participants = participants,
            LastMessageAtUtc = c.LastMessageAtUtc,
            LastMessagePreview = lastMessagePreview,
            UnreadCount = unreadCount,
            IsDeleted = c.IsDeleted,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }
}

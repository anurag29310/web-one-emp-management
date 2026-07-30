using System;

namespace EMS.Application.Features.Messaging.DTOs
{
    public class MessageParticipantDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime JoinedAtUtc { get; set; }
        public DateTime? LeftAtUtc { get; set; }
    }
}

using System;

namespace EMS.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Channel { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }
}

using System;

namespace EMS.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; } // recipient user
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Channel { get; set; } = "InApp"; // InApp, Email
        public bool IsRead { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }

        // audit
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}

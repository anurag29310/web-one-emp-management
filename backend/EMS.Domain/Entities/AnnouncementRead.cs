using System;

namespace EMS.Domain.Entities
{
    public class AnnouncementRead
    {
        public Guid Id { get; set; }
        public Guid AnnouncementId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ReadAtUtc { get; set; }
    }
}

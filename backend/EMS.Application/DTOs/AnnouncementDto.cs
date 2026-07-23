using System;

namespace EMS.Application.DTOs
{
    public class AnnouncementDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Priority { get; set; } = null!;
        public string AudienceType { get; set; } = null!;
        public Guid? DepartmentId { get; set; }
        public string? TargetRole { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public bool IsReadByMe { get; set; }
    }
}

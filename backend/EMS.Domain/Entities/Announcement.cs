using System;

namespace EMS.Domain.Entities
{
    public class Announcement
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Priority { get; set; } = "Normal"; // Normal, Important, Critical
        public string AudienceType { get; set; } = "All"; // All, Department, Role
        public Guid? DepartmentId { get; set; } // set when AudienceType == Department
        public string? TargetRole { get; set; } // set when AudienceType == Role
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }

        // audit
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}

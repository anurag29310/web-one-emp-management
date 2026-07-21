using System;

namespace EMS.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string EntityName { get; set; } = null!;
        public Guid? EntityId { get; set; }
        public string Action { get; set; } = null!;
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

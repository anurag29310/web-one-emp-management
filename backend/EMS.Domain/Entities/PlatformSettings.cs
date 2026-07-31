using System;

namespace EMS.Domain.Entities
{
    /// <summary>Singleton row (fixed seeded Id, no Create/Delete commands exist for this entity) holding
    /// platform-wide toggles that apply across every company.</summary>
    public class PlatformSettings
    {
        public static readonly Guid SingletonId = new("99999999-9999-9999-9999-999999999999");

        public Guid Id { get; set; } = SingletonId;
        public bool IsPublicRegistrationEnabled { get; set; } = true;
        public bool RequireApprovalForNewCompanies { get; set; } = true;
        public DateTime UpdatedAtUtc { get; set; }
    }
}

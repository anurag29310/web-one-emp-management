using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace EMS.Persistence.Configurations
{
    public class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
    {
        public void Configure(EntityTypeBuilder<PlatformSettings> builder)
        {
            builder.ToTable("PlatformSettings");
            builder.HasKey(x => x.Id);

            // Singleton row — no Create/Delete commands exist for this entity, only Get/Update
            // against this fixed seeded Id.
            builder.HasData(new PlatformSettings
            {
                Id = PlatformSettings.SingletonId,
                IsPublicRegistrationEnabled = true,
                RequireApprovalForNewCompanies = true,
                UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}

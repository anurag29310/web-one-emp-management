using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityName).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
            builder.Property(x => x.OldValuesJson);
            builder.Property(x => x.NewValuesJson);
            builder.Property(x => x.IpAddress).HasMaxLength(64);
            builder.Property(x => x.UserAgent).HasMaxLength(500);
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasIndex(x => new { x.EntityName, x.EntityId, x.CreatedAtUtc })
                .HasDatabaseName("IX_AuditLogs_EntityName_EntityId_CreatedAtUtc");
            builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc })
                .HasDatabaseName("IX_AuditLogs_UserId_CreatedAtUtc");
            builder.HasIndex(x => new { x.Action, x.CreatedAtUtc })
                .HasDatabaseName("IX_AuditLogs_Action_CreatedAtUtc");
        }
    }
}

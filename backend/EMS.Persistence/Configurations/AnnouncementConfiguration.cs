using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
    {
        public void Configure(EntityTypeBuilder<Announcement> builder)
        {
            builder.ToTable("Announcements");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Title).HasMaxLength(250).IsRequired();
            builder.Property(a => a.Message).HasMaxLength(2000).IsRequired();
            builder.Property(a => a.Priority).HasMaxLength(50).IsRequired();
            builder.Property(a => a.AudienceType).HasMaxLength(50).IsRequired();
            builder.Property(a => a.TargetRole).HasMaxLength(50);
            builder.Property(a => a.CreatedAtUtc).IsRequired();
            builder.HasIndex(a => new { a.AudienceType, a.DepartmentId });
            builder.HasIndex(a => new { a.AudienceType, a.TargetRole });
            builder.HasIndex(a => a.CreatedAtUtc);
        }
    }
}

using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AnnouncementReadConfiguration : IEntityTypeConfiguration<AnnouncementRead>
    {
        public void Configure(EntityTypeBuilder<AnnouncementRead> builder)
        {
            builder.ToTable("AnnouncementReads");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ReadAtUtc).IsRequired();
            builder.HasIndex(r => new { r.AnnouncementId, r.UserId }).IsUnique();
        }
    }
}

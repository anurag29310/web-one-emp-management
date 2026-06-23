using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Title).HasMaxLength(250).IsRequired();
            builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
            builder.Property(n => n.Channel).HasMaxLength(50).IsRequired();
            builder.Property(n => n.CreatedAtUtc).IsRequired();
            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.CreatedAtUtc);
        }
    }
}

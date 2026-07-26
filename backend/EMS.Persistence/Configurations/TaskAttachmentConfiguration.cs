using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
    {
        public void Configure(EntityTypeBuilder<TaskAttachment> builder)
        {
            builder.ToTable("TaskAttachments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.BlobContainer).IsRequired().HasMaxLength(100);
            builder.Property(a => a.BlobPath).IsRequired().HasMaxLength(500);

            builder.HasIndex(a => a.TaskId);

            builder.HasOne<TaskItem>().WithMany().HasForeignKey(a => a.TaskId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

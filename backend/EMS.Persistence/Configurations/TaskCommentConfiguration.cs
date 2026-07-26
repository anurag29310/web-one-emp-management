using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
    {
        public void Configure(EntityTypeBuilder<TaskComment> builder)
        {
            builder.ToTable("TaskComments");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Comment).IsRequired().HasMaxLength(2000);

            builder.HasIndex(c => new { c.TaskId, c.CreatedAtUtc });

            builder.HasOne<TaskItem>().WithMany().HasForeignKey(c => c.TaskId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

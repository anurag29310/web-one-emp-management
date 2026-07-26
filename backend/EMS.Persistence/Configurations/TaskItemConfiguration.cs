using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.TaskNumber).IsRequired().HasMaxLength(20);
            builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Description).HasMaxLength(2000);
            builder.Property(t => t.Notes).HasMaxLength(1000);
            builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

            builder.HasIndex(t => t.TaskNumber).IsUnique();
            builder.HasIndex(t => t.AssignedEmployeeId);
            builder.HasIndex(t => t.ClientId);
            builder.HasIndex(t => t.Status);

            // Restrict (not cascade): task history must survive if a client or employee record changes.
            builder.HasOne(t => t.Client).WithMany().HasForeignKey(t => t.ClientId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(t => t.AssignedEmployee).WithMany().HasForeignKey(t => t.AssignedEmployeeId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(t => t.RowVersion).IsRowVersion();
        }
    }
}

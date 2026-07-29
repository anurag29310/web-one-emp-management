using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class PerformanceGoalConfiguration : IEntityTypeConfiguration<PerformanceGoal>
    {
        public void Configure(EntityTypeBuilder<PerformanceGoal> builder)
        {
            builder.ToTable("PerformanceGoals");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.GoalNumber).IsRequired().HasMaxLength(20);
            builder.Property(g => g.Title).IsRequired().HasMaxLength(200);
            builder.Property(g => g.Description).HasMaxLength(2000);
            builder.Property(g => g.Category).HasMaxLength(100);
            builder.Property(g => g.Weight).HasColumnType("decimal(5,2)");
            builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(g => g.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(g => g.GoalNumber).IsUnique();
            builder.HasIndex(g => g.EmployeeId);
            builder.HasIndex(g => g.Status);
            builder.HasIndex(g => g.IsDeleted);

            builder.HasOne(g => g.Employee).WithMany().HasForeignKey(g => g.EmployeeId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(g => g.RowVersion).IsRowVersion();
        }
    }
}

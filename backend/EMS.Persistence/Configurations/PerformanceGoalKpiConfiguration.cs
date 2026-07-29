using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class PerformanceGoalKpiConfiguration : IEntityTypeConfiguration<PerformanceGoalKpi>
    {
        public void Configure(EntityTypeBuilder<PerformanceGoalKpi> builder)
        {
            builder.ToTable("PerformanceGoalKpis");
            builder.HasKey(k => k.Id);
            builder.Property(k => k.Name).IsRequired().HasMaxLength(200);
            builder.Property(k => k.TargetValue).HasColumnType("decimal(18,2)");
            builder.Property(k => k.CurrentValue).HasColumnType("decimal(18,2)");
            builder.Property(k => k.Unit).HasMaxLength(30);
            builder.Property(k => k.Notes).HasMaxLength(1000);

            builder.HasIndex(k => k.GoalId);

            builder.HasOne(k => k.Goal).WithMany(g => g.Kpis).HasForeignKey(k => k.GoalId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

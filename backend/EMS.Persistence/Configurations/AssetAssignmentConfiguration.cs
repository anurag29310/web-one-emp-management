using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
    {
        public void Configure(EntityTypeBuilder<AssetAssignment> builder)
        {
            builder.ToTable("AssetAssignments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.ConditionAtAssignment).HasMaxLength(500);
            builder.Property(a => a.ConditionAtReturn).HasMaxLength(500);
            builder.Property(a => a.Notes).HasMaxLength(1000);

            builder.HasIndex(a => a.AssetId);
            builder.HasIndex(a => a.EmployeeId);
            builder.HasIndex(a => a.ReturnedDate);

            builder.HasOne(a => a.Asset).WithMany().HasForeignKey(a => a.AssetId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

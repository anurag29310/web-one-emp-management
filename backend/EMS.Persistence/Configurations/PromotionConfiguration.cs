using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.ToTable("Promotions");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PromotionNumber).IsRequired().HasMaxLength(20);
            builder.Property(p => p.Reason).IsRequired().HasMaxLength(1000);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(p => p.DecisionNotes).HasMaxLength(1000);
            builder.Property(p => p.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(p => p.PromotionNumber).IsUnique();
            builder.HasIndex(p => p.EmployeeId);
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.IsDeleted);

            builder.HasOne(p => p.Employee).WithMany().HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.FromDesignation).WithMany().HasForeignKey(p => p.FromDesignationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.ToDesignation).WithMany().HasForeignKey(p => p.ToDesignationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.FromDepartment).WithMany().HasForeignKey(p => p.FromDepartmentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(p => p.ToDepartment).WithMany().HasForeignKey(p => p.ToDepartmentId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(p => p.RowVersion).IsRowVersion();
        }
    }
}

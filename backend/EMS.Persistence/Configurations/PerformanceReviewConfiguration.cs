using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class PerformanceReviewConfiguration : IEntityTypeConfiguration<PerformanceReview>
    {
        public void Configure(EntityTypeBuilder<PerformanceReview> builder)
        {
            builder.ToTable("PerformanceReviews");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ReviewNumber).IsRequired().HasMaxLength(20);
            builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            builder.Property(r => r.SelfAssessment).HasMaxLength(4000);
            builder.Property(r => r.ManagerAssessment).HasMaxLength(4000);
            builder.Property(r => r.OverallRating).HasColumnType("decimal(3,2)");
            builder.Property(r => r.Notes).HasMaxLength(1000);
            builder.Property(r => r.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(r => r.ReviewNumber).IsUnique();
            builder.HasIndex(r => r.EmployeeId);
            builder.HasIndex(r => r.ReviewerEmployeeId);
            builder.HasIndex(r => r.Status);
            builder.HasIndex(r => r.IsDeleted);

            builder.HasOne(r => r.Employee).WithMany().HasForeignKey(r => r.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(r => r.ReviewerEmployee).WithMany().HasForeignKey(r => r.ReviewerEmployeeId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(r => r.RowVersion).IsRowVersion();
        }
    }
}

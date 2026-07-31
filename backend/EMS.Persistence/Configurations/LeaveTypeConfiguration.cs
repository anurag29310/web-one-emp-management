using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
    {
        public void Configure(EntityTypeBuilder<LeaveType> builder)
        {
            builder.ToTable("LeaveTypes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Code).HasMaxLength(50);
            builder.Property(x => x.AnnualEntitlementDays).HasColumnType("decimal(5,2)");
            builder.HasIndex(x => x.CompanyId);
            builder.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();

            builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

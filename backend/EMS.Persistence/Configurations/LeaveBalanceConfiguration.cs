using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
    {
        public void Configure(EntityTypeBuilder<LeaveBalance> builder)
        {
            builder.ToTable("LeaveBalances");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OpeningBalance).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Accrued).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Used).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Adjusted).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Available).HasColumnType("decimal(5,2)");
            builder.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
        }
    }
}

using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
    {
        public void Configure(EntityTypeBuilder<LeaveRequest> builder)
        {
            builder.ToTable("LeaveRequests");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Reason).HasMaxLength(500);
            builder.Property(x => x.TotalDays).HasColumnType("decimal(5,2)");
            builder.Property(x => x.Status).IsRequired();
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.Status);
        }
    }
}

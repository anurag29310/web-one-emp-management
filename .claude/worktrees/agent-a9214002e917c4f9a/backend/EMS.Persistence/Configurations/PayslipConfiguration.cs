using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
    {
        public void Configure(EntityTypeBuilder<Payslip> builder)
        {
            builder.ToTable("Payslips");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.EmployeeId).IsRequired();
            builder.Property(p => p.GeneratedAtUtc).IsRequired();
            builder.Property(p => p.NetPay).IsRequired();
            builder.HasIndex(p => p.EmployeeId);
            builder.HasIndex(p => p.PayrollRunId);
        }
    }
}

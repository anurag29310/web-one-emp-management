using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
    {
        public void Configure(EntityTypeBuilder<PayrollRun> builder)
        {
            builder.ToTable("PayrollRuns");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.PeriodStart).IsRequired();
            builder.Property(p => p.PeriodEnd).IsRequired();
            builder.Property(p => p.ProcessedAtUtc).IsRequired();
            builder.Property(p => p.ProcessedBy).IsRequired();
            builder.HasMany<Payslip>().WithOne().HasForeignKey(ps => ps.PayrollRunId);
        }
    }
}

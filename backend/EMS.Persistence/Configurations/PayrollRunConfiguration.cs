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

            // Bound to the PayrollRun.Payslips navigation explicitly — see the comment in
            // SalaryStructureConfiguration for why HasMany<Payslip>() without it produces a redundant,
            // always-null shadow FK column (PayrollRunId1) instead of reusing the real one.
            builder.HasMany(p => p.Payslips).WithOne().HasForeignKey(ps => ps.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

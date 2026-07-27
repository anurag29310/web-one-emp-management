using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
    {
        public void Configure(EntityTypeBuilder<SalaryStructure> builder)
        {
            builder.ToTable("SalaryStructures");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.BasicSalary).IsRequired();
            builder.Property(s => s.EffectiveFrom).IsRequired();

            builder.HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(s => s.EmployeeId);

            // Bound to the SalaryStructure.Allowances/Deductions navigation collections explicitly
            // (HasMany(s => s.Allowances), not the untyped HasMany<Allowance>()) — otherwise EF Core
            // treats the navigation as a second, undeclared relationship and materializes a redundant
            // shadow FK column (SalaryStructureId1) that's never populated, silently breaking
            // .Include(s => s.Allowances) against a real relational provider (it joins on the wrong,
            // always-null column). See EMS.Persistence Migrations/*_FixPayrollRelationships.
            builder.HasMany(s => s.Allowances).WithOne().HasForeignKey(a => a.SalaryStructureId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(s => s.Deductions).WithOne().HasForeignKey(d => d.SalaryStructureId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

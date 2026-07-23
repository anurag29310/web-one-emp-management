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
            builder.HasMany<Allowance>().WithOne().HasForeignKey(a => a.SalaryStructureId);
            builder.HasMany<Deduction>().WithOne().HasForeignKey(d => d.SalaryStructureId);
        }
    }
}

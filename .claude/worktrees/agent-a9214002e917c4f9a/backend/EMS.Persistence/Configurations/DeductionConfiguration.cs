using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class DeductionConfiguration : IEntityTypeConfiguration<Deduction>
    {
        public void Configure(EntityTypeBuilder<Deduction> builder)
        {
            builder.ToTable("Deductions");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
            builder.Property(d => d.Amount).IsRequired();
            builder.HasIndex(d => d.SalaryStructureId);
        }
    }
}

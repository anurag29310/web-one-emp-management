using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AllowanceConfiguration : IEntityTypeConfiguration<Allowance>
    {
        public void Configure(EntityTypeBuilder<Allowance> builder)
        {
            builder.ToTable("Allowances");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
            builder.Property(a => a.Amount).IsRequired();
            builder.HasIndex(a => a.SalaryStructureId);
        }
    }
}

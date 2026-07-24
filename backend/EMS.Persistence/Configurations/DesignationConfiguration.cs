using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
    {
        public void Configure(EntityTypeBuilder<Designation> builder)
        {
            builder.ToTable("Designations");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Name).IsRequired().HasMaxLength(150);
            builder.Property(d => d.Code).IsRequired().HasMaxLength(50);

            builder.HasIndex(d => d.Name).IsUnique();
            builder.HasIndex(d => d.Code).IsUnique();

            builder.Property(d => d.RowVersion).IsRowVersion();
        }
    }
}

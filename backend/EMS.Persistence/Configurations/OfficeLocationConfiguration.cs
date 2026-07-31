using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class OfficeLocationConfiguration : IEntityTypeConfiguration<OfficeLocation>
    {
        public void Configure(EntityTypeBuilder<OfficeLocation> builder)
        {
            builder.ToTable("OfficeLocations");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Name).IsRequired().HasMaxLength(150);
            builder.Property(o => o.Code).IsRequired().HasMaxLength(50);
            builder.Property(o => o.AddressLine1).HasMaxLength(250);
            builder.Property(o => o.AddressLine2).HasMaxLength(250);
            builder.Property(o => o.City).IsRequired().HasMaxLength(100);
            builder.Property(o => o.State).HasMaxLength(100);
            builder.Property(o => o.Country).IsRequired().HasMaxLength(100);
            builder.Property(o => o.TimeZoneId).IsRequired().HasMaxLength(100);
            builder.Property(o => o.Latitude).HasColumnType("decimal(9,6)");
            builder.Property(o => o.Longitude).HasColumnType("decimal(9,6)");

            builder.HasIndex(o => o.Code).IsUnique();

            builder.Property(o => o.RowVersion).IsRowVersion();
        }
    }
}

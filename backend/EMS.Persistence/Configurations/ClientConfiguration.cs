using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.ToTable("Clients");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.ClientName).IsRequired().HasMaxLength(150);
            builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(150);
            builder.Property(c => c.ContactPerson).IsRequired().HasMaxLength(150);
            builder.Property(c => c.MobileNumber).IsRequired().HasMaxLength(20);
            builder.Property(c => c.AlternateMobile).HasMaxLength(20);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(255);
            builder.Property(c => c.GstNumber).HasMaxLength(20);
            builder.Property(c => c.AddressLine1).IsRequired().HasMaxLength(250);
            builder.Property(c => c.AddressLine2).HasMaxLength(250);
            builder.Property(c => c.City).IsRequired().HasMaxLength(100);
            builder.Property(c => c.State).HasMaxLength(100);
            builder.Property(c => c.Country).IsRequired().HasMaxLength(100);
            builder.Property(c => c.PostalCode).IsRequired().HasMaxLength(20);
            builder.Property(c => c.Latitude).HasColumnType("decimal(9,6)");
            builder.Property(c => c.Longitude).HasColumnType("decimal(9,6)");
            builder.Property(c => c.Notes).HasMaxLength(1000);

            builder.HasIndex(c => c.ClientName).IsUnique();
            builder.HasIndex(c => c.IsActive);

            builder.Property(c => c.RowVersion).IsRowVersion();
        }
    }
}

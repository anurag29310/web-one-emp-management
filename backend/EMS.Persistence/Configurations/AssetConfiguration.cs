using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AssetConfiguration : IEntityTypeConfiguration<Asset>
    {
        public void Configure(EntityTypeBuilder<Asset> builder)
        {
            builder.ToTable("Assets");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.AssetTag).IsRequired().HasMaxLength(20);
            builder.Property(a => a.Category).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Brand).HasMaxLength(100);
            builder.Property(a => a.Model).HasMaxLength(100);
            builder.Property(a => a.SerialNumber).HasMaxLength(150);
            builder.Property(a => a.PurchaseCost).HasColumnType("decimal(18,2)");
            builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(a => a.Notes).HasMaxLength(1000);
            builder.Property(a => a.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(a => a.AssetTag).IsUnique();
            builder.HasIndex(a => a.Category);
            builder.HasIndex(a => a.Status);
            builder.HasIndex(a => a.IsDeleted);

            builder.Property(a => a.RowVersion).IsRowVersion();
        }
    }
}

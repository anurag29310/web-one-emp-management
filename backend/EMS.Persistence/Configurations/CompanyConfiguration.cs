using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(c => c.Timezone).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Currency).IsRequired().HasMaxLength(10);
            builder.Property(c => c.LogoUrl).HasMaxLength(500);
            builder.Property(c => c.SuspendedReason).HasMaxLength(500);
            builder.Property(c => c.RejectedReason).HasMaxLength(500);
            builder.Property(c => c.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(c => c.Status);
            builder.HasIndex(c => c.RegisteredAtUtc);

            builder.Property(c => c.RowVersion).IsRowVersion();
        }
    }
}

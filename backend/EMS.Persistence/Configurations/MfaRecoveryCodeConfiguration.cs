using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class MfaRecoveryCodeConfiguration : IEntityTypeConfiguration<MfaRecoveryCode>
    {
        public void Configure(EntityTypeBuilder<MfaRecoveryCode> builder)
        {
            builder.ToTable("MfaRecoveryCodes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CodeHash).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            builder.HasIndex(x => x.UserId);
        }
    }
}

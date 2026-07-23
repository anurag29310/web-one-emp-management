using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class MfaChallengeConfiguration : IEntityTypeConfiguration<MfaChallenge>
    {
        public void Configure(EntityTypeBuilder<MfaChallenge> builder)
        {
            builder.ToTable("MfaChallenges");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            builder.HasIndex(x => x.UserId);
        }
    }
}

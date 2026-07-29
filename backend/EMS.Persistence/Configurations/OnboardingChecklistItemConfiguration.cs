using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class OnboardingChecklistItemConfiguration : IEntityTypeConfiguration<OnboardingChecklistItem>
    {
        public void Configure(EntityTypeBuilder<OnboardingChecklistItem> builder)
        {
            builder.ToTable("OnboardingChecklistItems");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.ItemName).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Notes).HasMaxLength(500);

            builder.HasIndex(i => i.CandidateId);

            builder.HasOne(i => i.Candidate).WithMany().HasForeignKey(i => i.CandidateId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

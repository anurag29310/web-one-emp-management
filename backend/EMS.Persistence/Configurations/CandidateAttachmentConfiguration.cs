using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class CandidateAttachmentConfiguration : IEntityTypeConfiguration<CandidateAttachment>
    {
        public void Configure(EntityTypeBuilder<CandidateAttachment> builder)
        {
            builder.ToTable("CandidateAttachments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.BlobContainer).IsRequired().HasMaxLength(100);
            builder.Property(a => a.BlobPath).IsRequired().HasMaxLength(500);

            builder.HasIndex(a => a.CandidateId);

            builder.HasOne<Candidate>().WithMany().HasForeignKey(a => a.CandidateId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

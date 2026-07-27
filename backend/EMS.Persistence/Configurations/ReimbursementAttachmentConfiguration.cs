using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class ReimbursementAttachmentConfiguration : IEntityTypeConfiguration<ReimbursementAttachment>
    {
        public void Configure(EntityTypeBuilder<ReimbursementAttachment> builder)
        {
            builder.ToTable("ReimbursementAttachments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.BlobContainer).IsRequired().HasMaxLength(100);
            builder.Property(a => a.BlobPath).IsRequired().HasMaxLength(500);

            builder.HasIndex(a => a.ReimbursementId);

            builder.HasOne<Reimbursement>().WithMany().HasForeignKey(a => a.ReimbursementId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

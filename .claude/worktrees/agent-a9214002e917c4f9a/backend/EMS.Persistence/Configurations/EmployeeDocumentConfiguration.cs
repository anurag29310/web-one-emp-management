using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
    {
        public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
        {
            builder.ToTable("EmployeeDocuments");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
            builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(d => d.BlobContainer).HasMaxLength(100).IsRequired();
            builder.Property(d => d.BlobPath).HasMaxLength(500).IsRequired();
            builder.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
            builder.Property(d => d.FileSizeBytes).IsRequired();
            builder.Property(d => d.UploadedAtUtc).IsRequired();
            builder.HasIndex(d => d.EmployeeId);
            builder.HasIndex(d => d.DocumentType);
            builder.Property(d => d.RowVersion).IsRowVersion();
        }
    }
}

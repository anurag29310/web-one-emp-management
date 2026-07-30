using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class OfferConfiguration : IEntityTypeConfiguration<Offer>
    {
        public void Configure(EntityTypeBuilder<Offer> builder)
        {
            builder.ToTable("Offers");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.OfferNumber).IsRequired().HasMaxLength(20);
            builder.Property(o => o.OfferedSalary).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(o => o.Notes).HasMaxLength(1000);
            builder.Property(o => o.BlobContainer).HasMaxLength(100);
            builder.Property(o => o.BlobPath).HasMaxLength(500);

            builder.HasIndex(o => o.OfferNumber).IsUnique();
            builder.HasIndex(o => o.CandidateId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.ExpiresAtUtc);

            builder.HasOne(o => o.Candidate).WithMany().HasForeignKey(o => o.CandidateId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(o => o.Designation).WithMany().HasForeignKey(o => o.DesignationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(o => o.Department).WithMany().HasForeignKey(o => o.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

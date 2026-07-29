using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
    {
        public void Configure(EntityTypeBuilder<Candidate> builder)
        {
            builder.ToTable("Candidates");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.CandidateNumber).IsRequired().HasMaxLength(20);
            builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
            builder.Property(c => c.PhoneNumber).HasMaxLength(30);
            builder.Property(c => c.Source).HasMaxLength(100);
            builder.Property(c => c.Notes).HasMaxLength(1000);
            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(c => c.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(c => c.CandidateNumber).IsUnique();
            builder.HasIndex(c => c.Email);
            builder.HasIndex(c => c.Status);
            builder.HasIndex(c => c.DesignationId);
            builder.HasIndex(c => c.IsDeleted);

            // Restrict — a candidate's application history must survive if the designation/department
            // it referenced is later changed, matching Tasks' treatment of Client/Employee FKs.
            builder.HasOne(c => c.Designation).WithMany().HasForeignKey(c => c.DesignationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.Department).WithMany().HasForeignKey(c => c.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(c => c.ConvertedEmployee).WithMany().HasForeignKey(c => c.ConvertedEmployeeId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(c => c.RowVersion).IsRowVersion();
        }
    }
}

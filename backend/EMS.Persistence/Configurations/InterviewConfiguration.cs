using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
    {
        public void Configure(EntityTypeBuilder<Interview> builder)
        {
            builder.ToTable("Interviews");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Round).IsRequired().HasMaxLength(150);
            builder.Property(i => i.Mode).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(i => i.Outcome).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(i => i.Feedback).HasMaxLength(2000);

            builder.HasIndex(i => i.CandidateId);
            builder.HasIndex(i => i.InterviewerEmployeeId);
            builder.HasIndex(i => i.ScheduledAtUtc);

            builder.HasOne(i => i.Candidate).WithMany().HasForeignKey(i => i.CandidateId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(i => i.InterviewerEmployee).WithMany().HasForeignKey(i => i.InterviewerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

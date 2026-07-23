using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AttendanceCorrectionConfiguration : IEntityTypeConfiguration<AttendanceCorrection>
    {
        public void Configure(EntityTypeBuilder<AttendanceCorrection> builder)
        {
            builder.ToTable("AttendanceCorrections");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            builder.Property(x => x.DecisionComments).HasMaxLength(500);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.AttendanceRecord)
                .WithMany()
                .HasForeignKey(x => x.AttendanceRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_AttendanceCorrections_Status");
        }
    }
}

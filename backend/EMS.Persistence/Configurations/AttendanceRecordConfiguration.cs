using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
    {
        public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
        {
            builder.ToTable("AttendanceRecords");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AttendanceDate).HasColumnType("date").IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(x => x.Notes).HasMaxLength(500);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_AttendanceRecords_EmployeeId_AttendanceDate");

            builder.HasIndex(x => new { x.AttendanceDate, x.Status })
                .HasDatabaseName("IX_AttendanceRecords_AttendanceDate_Status");
        }
    }
}

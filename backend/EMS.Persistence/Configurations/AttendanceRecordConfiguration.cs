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

            builder.Property(x => x.CheckInLatitude).HasColumnType("decimal(9,6)");
            builder.Property(x => x.CheckInLongitude).HasColumnType("decimal(9,6)");
            builder.Property(x => x.CheckInAddress).HasMaxLength(500);
            builder.Property(x => x.CheckInDeviceInfo).HasMaxLength(255);
            builder.Property(x => x.CheckInIpAddress).HasMaxLength(64);

            builder.Property(x => x.CheckOutLatitude).HasColumnType("decimal(9,6)");
            builder.Property(x => x.CheckOutLongitude).HasColumnType("decimal(9,6)");
            builder.Property(x => x.CheckOutAddress).HasMaxLength(500);
            builder.Property(x => x.CheckOutDeviceInfo).HasMaxLength(255);
            builder.Property(x => x.CheckOutIpAddress).HasMaxLength(64);

            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Shift)
                .WithMany()
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_AttendanceRecords_EmployeeId_AttendanceDate");

            builder.HasIndex(x => new { x.AttendanceDate, x.Status })
                .HasDatabaseName("IX_AttendanceRecords_AttendanceDate_Status");

            builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate, x.Status })
                .HasDatabaseName("IX_AttendanceRecords_EmployeeId_AttendanceDate_Status");
        }
    }
}

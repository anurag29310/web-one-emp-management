using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class EmployeeShiftConfiguration : IEntityTypeConfiguration<EmployeeShift>
    {
        public void Configure(EntityTypeBuilder<EmployeeShift> builder)
        {
            builder.ToTable("EmployeeShifts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EffectiveFrom).HasColumnType("date").IsRequired();
            builder.Property(x => x.EffectiveTo).HasColumnType("date");
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Shift)
                .WithMany()
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom })
                .HasDatabaseName("IX_EmployeeShifts_EmployeeId_EffectiveFrom");
        }
    }
}

using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
    {
        public void Configure(EntityTypeBuilder<Shift> builder)
        {
            builder.ToTable("Shifts");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
            builder.Property(x => x.StartTime).HasColumnType("time").IsRequired();
            builder.Property(x => x.EndTime).HasColumnType("time").IsRequired();
            builder.Property(x => x.GraceMinutes).IsRequired();
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }
}

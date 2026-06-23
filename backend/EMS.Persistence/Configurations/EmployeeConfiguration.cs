using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Persistence.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.PhoneNumber).HasMaxLength(30);
            builder.Property(x => x.Designation).HasMaxLength(150);
            builder.Property(x => x.ProfilePhotoUrl).HasMaxLength(500);
            builder.Property(x => x.EmploymentStatus).HasMaxLength(50);

            builder.HasIndex(x => x.EmployeeCode).IsUnique();
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.DepartmentId);

            builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne< Domain.Entities.Employee >(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}

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
            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.EmployeeCode).IsRequired().HasMaxLength(50);
            builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.MiddleName).HasMaxLength(100);
            builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Email).HasMaxLength(256);
            builder.Property(x => x.PhoneNumber).HasMaxLength(30);
            builder.Property(x => x.AddressLine1).HasMaxLength(250);
            builder.Property(x => x.AddressLine2).HasMaxLength(250);
            builder.Property(x => x.City).HasMaxLength(100);
            builder.Property(x => x.State).HasMaxLength(100);
            builder.Property(x => x.PostalCode).HasMaxLength(20);
            builder.Property(x => x.Country).HasMaxLength(100);
            builder.Property(x => x.EmergencyContactName).HasMaxLength(150);
            builder.Property(x => x.EmergencyContactPhone).HasMaxLength(30);
            builder.Property(x => x.EmergencyContactRelation).HasMaxLength(100);
            builder.Property(x => x.EmploymentStatus).HasMaxLength(50);
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            builder.HasIndex(x => new { x.CompanyId, x.EmployeeCode }).IsUnique();
            builder.HasIndex(x => new { x.CompanyId, x.Email }).IsUnique();
            builder.HasIndex(x => x.CompanyId);
            builder.HasIndex(x => x.DepartmentId);
            builder.HasIndex(x => x.ManagerId);
            builder.HasIndex(x => x.DesignationId);
            builder.HasIndex(x => x.OfficeLocationId);

            builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(x => x.Designation).WithMany().HasForeignKey(x => x.DesignationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.OfficeLocation).WithMany().HasForeignKey(x => x.OfficeLocationId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ProfilePhotoDocument).WithMany().HasForeignKey(x => x.ProfilePhotoDocumentId).OnDelete(DeleteBehavior.SetNull);
            builder.HasOne<Domain.Entities.Employee>(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}

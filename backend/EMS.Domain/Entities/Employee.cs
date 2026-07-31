using System;

namespace EMS.Domain.Entities
{
    public class Employee
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Company? Company { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public DateTime JoinDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? TeamId { get; set; }
        public Guid DesignationId { get; set; }
        public Guid? ManagerId { get; set; }
        public Guid OfficeLocationId { get; set; }
        public Guid? ProfilePhotoDocumentId { get; set; }
        public string? EmploymentStatus { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ExitDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public Domain.Entities.Department? Department { get; set; }
        public Domain.Entities.Team? Team { get; set; }
        public Domain.Entities.Designation? Designation { get; set; }
        public Domain.Entities.OfficeLocation? OfficeLocation { get; set; }
        public Domain.Entities.Employee? Manager { get; set; }
        public Domain.Entities.EmployeeDocument? ProfilePhotoDocument { get; set; }
    }
}

using EMS.Application.Common.DTOs;
using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Employees.DTOs
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public AddressDto Address { get; set; } = new();
        public EmergencyContactDto EmergencyContact { get; set; } = new();
        public DateTime JoinDate { get; set; }
        public DateTime? ExitDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? TeamId { get; set; }
        public string? TeamName { get; set; }
        public Guid DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public Guid? ManagerId { get; set; }
        public Guid OfficeLocationId { get; set; }
        public string? OfficeLocationName { get; set; }
        public Guid? ProfilePhotoDocumentId { get; set; }
        public string? EmploymentStatus { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static EmployeeDto FromEntity(Employee e) => new()
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FirstName = e.FirstName,
            MiddleName = e.MiddleName,
            LastName = e.LastName,
            Email = e.Email,
            PhoneNumber = e.PhoneNumber,
            DateOfBirth = e.DateOfBirth,
            Gender = e.Gender,
            Address = new AddressDto
            {
                AddressLine1 = e.AddressLine1,
                AddressLine2 = e.AddressLine2,
                City = e.City,
                State = e.State,
                PostalCode = e.PostalCode,
                Country = e.Country
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = e.EmergencyContactName,
                Phone = e.EmergencyContactPhone,
                Relation = e.EmergencyContactRelation
            },
            JoinDate = e.JoinDate,
            ExitDate = e.ExitDate,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name,
            TeamId = e.TeamId,
            TeamName = e.Team?.Name,
            DesignationId = e.DesignationId,
            DesignationName = e.Designation?.Name,
            ManagerId = e.ManagerId,
            OfficeLocationId = e.OfficeLocationId,
            OfficeLocationName = e.OfficeLocation?.Name,
            ProfilePhotoDocumentId = e.ProfilePhotoDocumentId,
            EmploymentStatus = e.EmploymentStatus,
            IsActive = e.IsActive,
            IsDeleted = e.IsDeleted,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
    }
}

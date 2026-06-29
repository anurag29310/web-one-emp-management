using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Employees.DTOs
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? ExitDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Designation { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? EmploymentStatus { get; set; }
        public bool IsActive { get; set; }

        public static EmployeeDto FromEntity(Employee e) => new()
        {
            Id = e.Id,
            EmployeeCode = e.EmployeeCode,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            PhoneNumber = e.PhoneNumber,
            DateOfBirth = e.DateOfBirth,
            Gender = e.Gender,
            Address = e.Address,
            EmergencyContactName = e.EmergencyContactName,
            EmergencyContactNumber = e.EmergencyContactNumber,
            JoinDate = e.JoinDate,
            ExitDate = e.ExitDate,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name,
            Designation = e.Designation,
            ManagerId = e.ManagerId,
            ProfilePhotoUrl = e.ProfilePhotoUrl,
            EmploymentStatus = e.EmploymentStatus,
            IsActive = e.IsActive
        };
    }
}

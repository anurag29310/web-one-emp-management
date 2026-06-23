using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class CreateEmployeeCommand : IRequest<EMS.Domain.Entities.Employee>
    {
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public DateTime JoinDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? Designation { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? EmploymentStatus { get; set; }
    }
}

using EMS.Application.Common.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class CreateEmployeeCommand : IRequest<EMS.Domain.Entities.Employee>
    {
        public string EmployeeCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public AddressDto? Address { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
        public DateTime JoinDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? TeamId { get; set; }
        public Guid DesignationId { get; set; }
        public Guid? ManagerId { get; set; }
        public Guid OfficeLocationId { get; set; }
        public string? EmploymentStatus { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class UpdateEmployeeProfileCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
    }
}

using EMS.Application.Common.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class UpdateEmployeeProfileCommand : IRequest
    {
        public Guid Id { get; set; }
        public string? PhoneNumber { get; set; }
        public AddressDto? Address { get; set; }
        public EmergencyContactDto? EmergencyContact { get; set; }
    }
}

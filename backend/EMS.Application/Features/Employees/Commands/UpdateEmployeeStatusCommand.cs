using MediatR;
using System;

namespace EMS.Application.Features.Employees.Commands
{
    public class UpdateEmployeeStatusCommand : IRequest
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ExitDate { get; set; }
        public string? Reason { get; set; }
    }
}

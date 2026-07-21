using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    /// <summary>Ends (soft-deletes) an employee's shift assignment (api-specification.md 8.9).</summary>
    public class EndEmployeeShiftCommand : IRequest
    {
        public Guid EmployeeId { get; set; }
        public Guid AssignmentId { get; set; }
    }
}

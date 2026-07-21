using EMS.Application.Features.Attendance.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Attendance.Queries
{
    /// <summary>List an employee's shift assignments. Access is Admin/HR/Manager-for-team/Employee-self
    /// (api-specification.md 8.9).</summary>
    public class GetEmployeeShiftsQuery : IRequest<IEnumerable<EmployeeShiftDto>>
    {
        public Guid EmployeeId { get; set; }
        public Guid RequestingUserId { get; set; }
        public bool IsAdminOrHr { get; set; }
        public bool IsManager { get; set; }
    }
}

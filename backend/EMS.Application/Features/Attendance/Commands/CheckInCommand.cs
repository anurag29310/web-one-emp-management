using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class CheckInCommand : IRequest<AttendanceRecord>
    {
        public Guid EmployeeId { get; set; }
        public DateTime CheckInAtUtc { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set by the controller from the caller's identity.</summary>
        public Guid RequestingUserId { get; set; }

        /// <summary>True when the caller holds an Admin/HR role and may record on behalf of another employee.</summary>
        public bool IsPrivileged { get; set; }
    }
}

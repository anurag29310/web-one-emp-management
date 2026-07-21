using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    public class CheckOutCommand : IRequest<AttendanceRecord>
    {
        public Guid EmployeeId { get; set; }
        public DateTime CheckOutAtUtc { get; set; }
        public string? Notes { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

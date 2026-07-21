using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    /// <summary>Manual attendance record creation by Admin/HR (api-specification.md 8.5).</summary>
    public class CreateAttendanceRecordCommand : IRequest<AttendanceRecord>
    {
        public Guid EmployeeId { get; set; }
        public Guid? ShiftId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInAtUtc { get; set; }
        public DateTime? CheckOutAtUtc { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
    }
}

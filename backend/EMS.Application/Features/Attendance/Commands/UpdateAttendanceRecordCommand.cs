using EMS.Domain.Entities;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Commands
{
    /// <summary>Manual correction by Admin/HR (api-specification.md 8.6).</summary>
    public class UpdateAttendanceRecordCommand : IRequest<AttendanceRecord>
    {
        public Guid Id { get; set; }
        public Guid? ShiftId { get; set; }
        public DateTime? CheckInAtUtc { get; set; }
        public DateTime? CheckOutAtUtc { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
    }
}

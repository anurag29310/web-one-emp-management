using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Attendance.DTOs
{
    public class AttendanceRecordDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? ShiftId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInAtUtc { get; set; }
        public DateTime? CheckOutAtUtc { get; set; }
        public string Status { get; set; } = null!;
        public bool IsLateArrival { get; set; }
        public bool IsEarlyLeave { get; set; }
        public int? TotalWorkMinutes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static AttendanceRecordDto FromEntity(AttendanceRecord a) => new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            ShiftId = a.ShiftId,
            AttendanceDate = a.AttendanceDate,
            CheckInAtUtc = a.CheckInAtUtc,
            CheckOutAtUtc = a.CheckOutAtUtc,
            Status = a.Status.ToString(),
            IsLateArrival = a.IsLateArrival,
            IsEarlyLeave = a.IsEarlyLeave,
            TotalWorkMinutes = a.TotalWorkMinutes,
            Notes = a.Notes,
            CreatedAtUtc = a.CreatedAtUtc,
            UpdatedAtUtc = a.UpdatedAtUtc
        };
    }
}

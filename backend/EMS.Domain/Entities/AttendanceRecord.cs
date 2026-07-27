using System;
using EMS.Domain.Enums;

namespace EMS.Domain.Entities
{
    public class AttendanceRecord
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid? ShiftId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? CheckInAtUtc { get; set; }
        public DateTime? CheckOutAtUtc { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
        public bool IsLateArrival { get; set; }
        public bool IsEarlyLeave { get; set; }
        public int? TotalWorkMinutes { get; set; }
        public string? Notes { get; set; }

        public decimal? CheckInLatitude { get; set; }
        public decimal? CheckInLongitude { get; set; }
        public string? CheckInAddress { get; set; }
        public string? CheckInDeviceInfo { get; set; }
        public string? CheckInIpAddress { get; set; }

        public decimal? CheckOutLatitude { get; set; }
        public decimal? CheckOutLongitude { get; set; }
        public string? CheckOutAddress { get; set; }
        public string? CheckOutDeviceInfo { get; set; }
        public string? CheckOutIpAddress { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public Employee? Employee { get; set; }
        public Shift? Shift { get; set; }
    }
}

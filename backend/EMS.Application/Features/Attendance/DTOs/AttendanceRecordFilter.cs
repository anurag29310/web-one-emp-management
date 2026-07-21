using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Attendance.DTOs
{
    /// <summary>Query-side filter for attendance records; <see cref="EmployeeIdsScope"/> is populated by the
    /// handler (never bound from the request) to enforce self/team scoping regardless of the caller's filters.</summary>
    public class AttendanceRecordFilter
    {
        public Guid? EmployeeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public bool? IsLateArrival { get; set; }
        public bool? IsEarlyLeave { get; set; }
        public IEnumerable<Guid>? EmployeeIdsScope { get; set; }
    }
}

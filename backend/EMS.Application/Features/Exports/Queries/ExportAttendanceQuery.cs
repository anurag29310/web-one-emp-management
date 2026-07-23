using EMS.Application.Common.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Exports.Queries
{
    /// <summary>Export attendance records matching the same filters as <see cref="Attendance.Queries.GetAttendanceRecordsQuery"/> to Excel.</summary>
    public class ExportAttendanceQuery : IRequest<ExportFileResult>
    {
        public Guid? EmployeeId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? ManagerId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Status { get; set; }
        public bool? IsLateArrival { get; set; }
        public bool? IsEarlyLeave { get; set; }

        /// <summary>Set by the controller from the caller's identity, never bound from the query string.</summary>
        public Guid RequestingUserId { get; set; }
        public bool IsAdminOrHr { get; set; }
        public bool IsManager { get; set; }
    }
}

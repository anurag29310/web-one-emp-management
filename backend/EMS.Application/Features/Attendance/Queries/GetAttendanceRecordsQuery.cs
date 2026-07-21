using EMS.Application.Common.DTOs;
using EMS.Application.Features.Attendance.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Queries
{
    public class GetAttendanceRecordsQuery : IRequest<PagedResult<AttendanceRecordDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
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

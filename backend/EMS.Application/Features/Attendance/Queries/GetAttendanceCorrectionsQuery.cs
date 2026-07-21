using EMS.Application.Common.DTOs;
using EMS.Application.Features.Attendance.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Attendance.Queries
{
    /// <summary>List corrections. Access is Admin/HR/Manager (api-specification.md 8.8); Managers are
    /// scoped to corrections requested by their direct reports.</summary>
    public class GetAttendanceCorrectionsQuery : IRequest<PagedResult<AttendanceCorrectionDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? EmployeeId { get; set; }
        public string? Status { get; set; }

        public Guid RequestingUserId { get; set; }
        public bool IsAdminOrHr { get; set; }
        public bool IsManager { get; set; }
    }
}

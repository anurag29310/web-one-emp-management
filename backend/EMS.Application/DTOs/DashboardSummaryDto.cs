using System;
using System.Collections.Generic;

namespace EMS.Application.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public LeaveSummaryDto LeaveSummary { get; set; } = new LeaveSummaryDto();
        public AttendanceSummaryDto AttendanceSummary { get; set; } = new AttendanceSummaryDto();
        public IEnumerable<DepartmentSummaryDto> DepartmentSummaries { get; set; } = Array.Empty<DepartmentSummaryDto>();
    }

    public class LeaveSummaryDto
    {
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }
    }

    public class AttendanceSummaryDto
    {
        // Attendance module not implemented in backend yet; keep fields nullable/zero
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
    }

    public class DepartmentSummaryDto
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
    }
}

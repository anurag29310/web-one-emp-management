using System;
using System.Collections.Generic;

namespace EMS.Application.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public AttendanceSummaryDto Attendance { get; set; } = new AttendanceSummaryDto();
        public LeaveSummaryDto Leave { get; set; } = new LeaveSummaryDto();
        public IEnumerable<DepartmentSummaryDto> Departments { get; set; } = Array.Empty<DepartmentSummaryDto>();
    }

    public class AttendanceSummaryDto
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int OnLeave { get; set; }
    }

    public class LeaveSummaryDto
    {
        public int Pending { get; set; }
        public int ApprovedToday { get; set; }
        public int RejectedToday { get; set; }
    }

    public class DepartmentSummaryDto
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int ActiveEmployees { get; set; }
    }
}

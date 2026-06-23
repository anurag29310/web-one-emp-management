using System;

namespace EMS.Application.DTOs.Reports
{
    public class DashboardReportsDto
    {
        public int Headcount { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public LeaveSummaryReportDto LeaveSummary { get; set; } = new LeaveSummaryReportDto();
    }
}

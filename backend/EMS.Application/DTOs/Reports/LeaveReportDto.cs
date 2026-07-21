namespace EMS.Application.DTOs.Reports
{
    public class LeaveSummaryReportDto
    {
        public int TotalRequests { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }
}

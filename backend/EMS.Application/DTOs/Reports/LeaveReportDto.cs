using System;
using System.Collections.Generic;

namespace EMS.Application.DTOs.Reports
{
    public class LeaveSummaryReportDto
    {
        public int TotalRequests { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
    }

    public class LeaveBalanceDto
    {
        public Guid EmployeeId { get; set; }
        public Guid LeaveTypeId { get; set; }
        public decimal Available { get; set; }
    }
}

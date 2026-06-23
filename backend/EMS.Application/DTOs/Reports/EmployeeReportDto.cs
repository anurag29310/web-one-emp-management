using System;
using System.Collections.Generic;

namespace EMS.Application.DTOs.Reports
{
    public class EmployeeReportDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
    }

    public class DepartmentCountDto
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
    }

    public class EmployeeJoinExitDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public DateTime? ExitDate { get; set; }
    }
}

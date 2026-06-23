using EMS.Application.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IReportRepository
    {
        Task<EmployeeReportDto> GetEmployeeReportAsync();
        Task<IEnumerable<DepartmentCountDto>> GetDepartmentCountsAsync();
        Task<IEnumerable<EmployeeJoinExitDto>> GetEmployeeJoinExitAsync(DateTime from, DateTime to);
        Task<LeaveSummaryReportDto> GetLeaveSummaryAsync(DateTime from, DateTime to);
        Task<DashboardReportsDto> GetDashboardReportsAsync(DateTime asOf);
        // Attendance reports are not available until attendance module is implemented; return placeholders
    }
}

using EMS.Application.DTOs.Reports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IReportRepository
    {
        Task<EmployeeReportDto> GetEmployeeReportAsync(CancellationToken ct = default);
        Task<IEnumerable<DepartmentCountDto>> GetDepartmentCountsAsync(CancellationToken ct = default);
        Task<IEnumerable<EmployeeJoinExitDto>> GetEmployeeJoinExitAsync(DateTime from, DateTime to, CancellationToken ct = default);
        Task<LeaveSummaryReportDto> GetLeaveSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }
}

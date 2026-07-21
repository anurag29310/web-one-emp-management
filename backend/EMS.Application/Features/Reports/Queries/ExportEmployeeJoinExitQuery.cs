using MediatR;
using System;

namespace EMS.Application.Features.Reports.Queries
{
    /// <summary>Export the employee turnover (joiners/leavers) report as CSV.</summary>
    public class ExportEmployeeJoinExitQuery : IRequest<ReportExportResult>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}

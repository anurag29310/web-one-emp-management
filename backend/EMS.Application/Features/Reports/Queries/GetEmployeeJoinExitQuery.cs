using EMS.Application.DTOs.Reports;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Reports.Queries
{
    /// <summary>Employee turnover (joiners/leavers) report for a date range.</summary>
    public class GetEmployeeJoinExitQuery : IRequest<IEnumerable<EmployeeJoinExitDto>>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}

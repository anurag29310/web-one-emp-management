using EMS.Application.DTOs.Reports;
using EMS.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize(Policy = "CanViewReports")]
    [Produces("application/json")]
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator) => _mediator = mediator;

        /// <summary>Total, active, and inactive employee counts.</summary>
        [HttpGet("employees")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeReportDto>), 200)]
        public async Task<IActionResult> EmployeeReport(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEmployeeReportQuery(), ct);
            return Ok(ApiResponse<EmployeeReportDto>.Success(result));
        }

        /// <summary>Employee headcount grouped by department.</summary>
        [HttpGet("departments")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DepartmentCountDto>>), 200)]
        public async Task<IActionResult> DepartmentCounts(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetDepartmentCountsQuery(), ct);
            return Ok(ApiResponse<IEnumerable<DepartmentCountDto>>.Success(result));
        }

        /// <summary>Export the department headcount report as CSV.</summary>
        [HttpGet("departments/export")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ExportDepartmentCounts(CancellationToken ct)
        {
            var result = await _mediator.Send(new ExportDepartmentCountsQuery(), ct);
            return File(result.Content, result.ContentType, result.FileName);
        }

        /// <summary>Leave request counts by status for a date range.</summary>
        [HttpGet("leave-summary")]
        [ProducesResponseType(typeof(ApiResponse<LeaveSummaryReportDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> LeaveSummary([FromQuery] GetLeaveSummaryQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<LeaveSummaryReportDto>.Success(result));
        }

        /// <summary>Employee turnover (joiners and leavers) for a date range.</summary>
        [HttpGet("employee-turnover")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeJoinExitDto>>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> EmployeeTurnover([FromQuery] GetEmployeeJoinExitQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<IEnumerable<EmployeeJoinExitDto>>.Success(result));
        }

        /// <summary>Export the employee turnover report as CSV.</summary>
        [HttpGet("employee-turnover/export")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> ExportEmployeeTurnover([FromQuery] ExportEmployeeJoinExitQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return File(result.Content, result.ContentType, result.FileName);
        }
    }
}

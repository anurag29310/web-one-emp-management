using EMS.Application.Features.Exports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/exports")]
    [Authorize]
    public class ExportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ExportsController(IMediator mediator) => _mediator = mediator;

        /// <summary>Export employees matching the given filters to Excel.</summary>
        [HttpGet("employees")]
        [Authorize(Policy = "CanManageEmployees")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ExportEmployees([FromQuery] ExportEmployeesQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return File(result.Content, result.ContentType, result.FileName);
        }

        /// <summary>Export attendance records matching the given filters to Excel. Manager-role
        /// callers are scoped to their own team.</summary>
        [HttpGet("attendance")]
        [Authorize(Policy = "CanViewReports")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> ExportAttendance([FromQuery] ExportAttendanceQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsAdminOrHr = IsAdminOrHr();
            query.IsManager = User.IsInRole("Manager");
            var result = await _mediator.Send(query, ct);
            return File(result.Content, result.ContentType, result.FileName);
        }

        /// <summary>Export leave requests matching the given filters to Excel.</summary>
        [HttpGet("leave-requests")]
        [Authorize(Policy = "CanViewReports")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ExportLeaveRequests([FromQuery] ExportLeaveRequestsQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return File(result.Content, result.ContentType, result.FileName);
        }

        /// <summary>Export the dashboard summary matching the given filters to PDF.</summary>
        [HttpGet("dashboard-summary")]
        [Authorize(Policy = "CanViewReports")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> ExportDashboardSummary([FromQuery] ExportDashboardSummaryQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return File(result.Content, result.ContentType, result.FileName);
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("User identity could not be resolved.");

            return id;
        }

        private bool IsAdminOrHr() => User.IsInRole("Admin") || User.IsInRole("HR");
    }
}

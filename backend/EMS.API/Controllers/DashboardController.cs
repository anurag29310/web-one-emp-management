using EMS.Application.DTOs;
using EMS.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize(Policy = "CanViewDashboard")]
    [Produces("application/json")]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get employee, attendance, leave, and department metrics for the dashboard,
        /// optionally scoped to a department and/or date (defaults to today).
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> GetSummary([FromQuery] GetDashboardSummaryQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<DashboardSummaryDto>.Success(result));
        }
    }
}

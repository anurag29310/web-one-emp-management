using EMS.Application.DTOs;
using EMS.Application.Features.PlatformDashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>Super Admin's cross-company overview — counts and recent registrations, not
    /// tenant-scoped like every other dashboard in the system.</summary>
    [ApiController]
    [Route("api/v1/platform/dashboard")]
    [Authorize(Policy = "IsSuperAdmin")]
    [Produces("application/json")]
    public class PlatformDashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlatformDashboardController(IMediator mediator) => _mediator = mediator;

        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<PlatformDashboardSummaryDto>), 200)]
        public async Task<IActionResult> GetSummary([FromQuery] int recentCount, CancellationToken ct)
        {
            var query = new GetPlatformDashboardSummaryQuery();
            if (recentCount > 0) query.RecentCount = recentCount;

            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PlatformDashboardSummaryDto>.Success(result));
        }
    }
}

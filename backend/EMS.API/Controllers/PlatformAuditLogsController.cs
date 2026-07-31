using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using EMS.Application.Features.AuditLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>Super Admin's cross-company audit trail — optionally filter by a specific
    /// company id to drill into one tenant.</summary>
    [ApiController]
    [Route("api/v1/platform/audit-logs")]
    [Authorize(Policy = "IsSuperAdmin")]
    [Produces("application/json")]
    public class PlatformAuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlatformAuditLogsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetPlatformAuditLogsQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<AuditLogDto>>.Success(result));
        }
    }
}

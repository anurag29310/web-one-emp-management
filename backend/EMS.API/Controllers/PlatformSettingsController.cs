using EMS.Application.Features.Companies.Commands;
using EMS.Application.Features.Companies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>Platform-wide toggles — currently just the two registration switches. Only ever
    /// reads/writes the single seeded PlatformSettings row.</summary>
    [ApiController]
    [Route("api/v1/platform/settings")]
    [Authorize(Policy = "IsSuperAdmin")]
    [Produces("application/json")]
    public class PlatformSettingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlatformSettingsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PlatformSettingsDto>), 200)]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetPlatformSettingsQuery(), ct);
            return Ok(ApiResponse<PlatformSettingsDto>.Success(result));
        }

        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<PlatformSettingsDto>), 200)]
        public async Task<IActionResult> Update([FromBody] UpdatePlatformSettingsCommand cmd, CancellationToken ct)
        {
            await _mediator.Send(cmd, ct);
            var result = await _mediator.Send(new GetPlatformSettingsQuery(), ct);
            return Ok(ApiResponse<PlatformSettingsDto>.Success(result, "Platform settings updated."));
        }
    }
}

using EMS.Application.DTOs;
using EMS.Application.Features.Health.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("health")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HealthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Basic API health check. Confirms the process is running and able to serve requests.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), 200)]
        public IActionResult GetHealth()
        {
            var status = new HealthStatusDto { Status = "Healthy", TimestampUtc = DateTime.UtcNow };
            return Ok(ApiResponse<HealthStatusDto>.Success(status));
        }

        /// <summary>
        /// Liveness check for container orchestrators: confirms the process itself has not deadlocked or crashed.
        /// </summary>
        [HttpGet("live")]
        [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), 200)]
        public IActionResult GetLiveness()
        {
            var status = new HealthStatusDto { Status = "Healthy", TimestampUtc = DateTime.UtcNow };
            return Ok(ApiResponse<HealthStatusDto>.Success(status));
        }

        /// <summary>
        /// Readiness check: confirms the API can serve traffic, including connectivity to the database.
        /// </summary>
        [HttpGet("ready")]
        [ProducesResponseType(typeof(ApiResponse<ReadinessStatusDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ReadinessStatusDto>), 503)]
        public async Task<IActionResult> GetReadiness(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetReadinessQuery(), ct);

            if (!result.DatabaseConnected)
            {
                return StatusCode(503, ApiResponse<ReadinessStatusDto>.Success(result, "Service is not ready."));
            }

            return Ok(ApiResponse<ReadinessStatusDto>.Success(result));
        }
    }
}

using EMS.Application.Features.Companies.Commands;
using EMS.Application.Features.Companies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>Public, unauthenticated company self-registration — the entry point for a new
    /// tenant onto the platform. Gated by PlatformSettings.IsPublicRegistrationEnabled; lands in
    /// PendingApproval or Trial depending on PlatformSettings.RequireApprovalForNewCompanies.</summary>
    [ApiController]
    [Route("api/v1/company-registration")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class CompanyRegistrationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CompanyRegistrationController(IMediator mediator) => _mediator = mediator;

        /// <summary>Whether the public registration form should currently be shown.</summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> GetStatus(CancellationToken ct)
        {
            var enabled = await _mediator.Send(new GetPublicRegistrationStatusQuery(), ct);
            return Ok(ApiResponse<bool>.Success(enabled));
        }

        /// <summary>Register a new company and its first Admin user.</summary>
        [HttpPost]
        [EnableRateLimiting("RegisterPolicy")]
        [ProducesResponseType(typeof(ApiResponse<RegisterCompanyResult>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(typeof(ApiErrorResponse), 429)]
        public async Task<IActionResult> Register([FromBody] RegisterCompanyCommand cmd, CancellationToken ct)
        {
            var result = await _mediator.Send(cmd, ct);
            return StatusCode(201, ApiResponse<RegisterCompanyResult>.Success(result,
                result.RequiresApproval
                    ? "Company registered. Awaiting Super Admin approval before you can log in."
                    : "Company registered successfully."));
        }
    }
}

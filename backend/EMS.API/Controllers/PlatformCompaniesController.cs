using EMS.Application.Common.DTOs;
using EMS.Application.Features.Companies.Commands;
using EMS.Application.Features.Companies.DTOs;
using EMS.Application.Features.Companies.Queries;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>Super Admin's Company Management surface — entirely separate from every tenant
    /// policy/controller in the system (Super Admin manages companies, not day-to-day HR data
    /// inside a tenant).</summary>
    [ApiController]
    [Route("api/v1/platform/companies")]
    [Authorize(Policy = "IsSuperAdmin")]
    [Produces("application/json")]
    public class PlatformCompaniesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAuthRepository _authRepo;

        public PlatformCompaniesController(IMediator mediator, IAuthRepository authRepo)
        {
            _mediator = mediator;
            _authRepo = authRepo;
        }

        /// <summary>List companies — paginated, filterable by status, searchable by name.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CompanyDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetCompaniesQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<CompanyDto>>.Success(result));
        }

        /// <summary>Get a single company's detail, including employee count and admin list.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CompanyDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var company = await _mediator.Send(new GetCompanyByIdQuery { Id = id }, ct);
            if (company == null) return NotFound();
            return Ok(ApiResponse<CompanyDetailDto>.Success(company));
        }

        /// <summary>Directly create a company (no approval gate — lands in Active).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CompanyDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateCompanyCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = CompanyDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, ApiResponse<CompanyDto>.Success(dto, "Company created successfully."));
        }

        /// <summary>Update a company's basic information.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CompanyDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<CompanyDto>.Success(CompanyDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a company.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteCompanyCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted company.</summary>
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreCompanyCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Activate a company (Suspended/Inactive -> Active).</summary>
        [HttpPost("{id:guid}/activate")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ActivateCompanyCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Suspend a company — immediately force-logs-out every one of its users.</summary>
        [HttpPost("{id:guid}/suspend")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendCompanyRequest? body, CancellationToken ct)
        {
            await _mediator.Send(new SuspendCompanyCommand { Id = id, Reason = body?.Reason }, ct);
            return NoContent();
        }

        /// <summary>Approve a company still awaiting registration approval (PendingApproval -> Trial).</summary>
        [HttpPost("{id:guid}/approve")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ApproveCompanyCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Reject a company still awaiting registration approval (PendingApproval -> Rejected).</summary>
        [HttpPost("{id:guid}/reject")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectCompanyRequest? body, CancellationToken ct)
        {
            await _mediator.Send(new RejectCompanyCommand { Id = id, Reason = body?.Reason }, ct);
            return NoContent();
        }

        /// <summary>Force-logout every user of this company without changing its status.</summary>
        [HttpPost("{id:guid}/force-logout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ForceLogout(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ForceLogoutCompanyCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Issue a password reset token for one of this company's Admin users, reusing the same self-service forgot-password mechanism.</summary>
        [HttpPost("{id:guid}/admins/{userId:guid}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetAdminPassword(Guid id, Guid userId, CancellationToken ct)
        {
            var user = await _authRepo.GetByIdAsync(userId, ct);
            if (user == null || user.CompanyId != id)
                return NotFound();

            var token = await _authRepo.CreatePasswordResetTokenAsync(userId, ct);
            return Ok(ApiResponse<string>.Success(token, "Password reset token issued."));
        }
    }

    public class SuspendCompanyRequest
    {
        public string? Reason { get; set; }
    }

    public class RejectCompanyRequest
    {
        public string? Reason { get; set; }
    }
}

using EMS.Application.Common.DTOs;
using EMS.Application.Features.Assets.Commands;
using EMS.Application.Features.Assets.DTOs;
using EMS.Application.Features.Assets.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>
    /// Asset Management: Laptop/Mobile Allocation, Asset Return Tracking. All actions require the
    /// CanManageAssets policy (Admin, HR) — there is no self-service angle here (an employee doesn't
    /// manage their own asset assignments), unlike Task Management or Reimbursements.
    /// </summary>
    [ApiController]
    [Route("api/v1/assets")]
    [Authorize(Policy = "CanManageAssets")]
    [Produces("application/json")]
    public class AssetsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssetsController(IMediator mediator) => _mediator = mediator;

        // ─── Assets ────────────────────────────────────────────────────────────────

        /// <summary>List assets — paginated, filterable by status, category, search.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AssetDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAssetsQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<AssetDto>>.Success(result));
        }

        /// <summary>Get a single asset by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AssetDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var asset = await _mediator.Send(new GetAssetByIdQuery { Id = id }, ct);
            if (asset == null) return NotFound();
            return Ok(ApiResponse<AssetDto>.Success(AssetDto.FromEntity(asset)));
        }

        /// <summary>Register a new asset. Status always starts Available.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateAssetCommand cmd, CancellationToken ct)
        {
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<object>.Success(new { id }, "Asset created successfully."));
        }

        /// <summary>Update asset details. Does not change Status — use the dedicated status/assign/return actions.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssetCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Soft-delete an asset. Rejected if the asset is currently assigned.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteAssetCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted asset.</summary>
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreAssetCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Change an asset's status outside of the assign/return flow (e.g. mark UnderRepair, Retired, Lost, or back to Available). Rejected while the asset is currently assigned.</summary>
        [HttpPost("{id:guid}/status")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAssetStatusCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        // ─── Assignments (Allocation / Return Tracking) ───────────────────────────────

        /// <summary>List an asset's full assignment history, most recent first.</summary>
        [HttpGet("{id:guid}/assignments")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AssetAssignmentDto>>), 200)]
        public async Task<IActionResult> GetAssignments(Guid id, CancellationToken ct)
        {
            var assignments = await _mediator.Send(new GetAssetAssignmentsQuery { AssetId = id }, ct);
            return Ok(ApiResponse<IEnumerable<AssetAssignmentDto>>.Success(assignments));
        }

        /// <summary>Allocate an Available asset to an employee (Laptop/Mobile Allocation).</summary>
        [HttpPost("{id:guid}/assign")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignAssetCommand cmd, CancellationToken ct)
        {
            cmd.AssetId = id;
            cmd.RequestingUserId = GetCurrentUserId();
            var assignmentId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(assignmentId, "Asset assigned."));
        }

        /// <summary>Return an outstanding assignment (Asset Return Tracking). Sets the asset's resulting status (defaults to Available).</summary>
        [HttpPost("~/api/v1/asset-assignments/{id:guid}/return")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Return(Guid id, [FromBody] ReturnAssetCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>An employee's full asset assignment history (current and past).</summary>
        [HttpGet("~/api/v1/employees/{employeeId:guid}/assets")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<AssetAssignmentDto>>), 200)]
        public async Task<IActionResult> GetEmployeeAssets(Guid employeeId, CancellationToken ct)
        {
            var assignments = await _mediator.Send(new GetEmployeeAssetAssignmentsQuery { EmployeeId = employeeId }, ct);
            return Ok(ApiResponse<IEnumerable<AssetAssignmentDto>>.Success(assignments));
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("User identity could not be resolved.");
            return id;
        }
    }
}

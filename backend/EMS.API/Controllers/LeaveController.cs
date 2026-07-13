using EMS.Application.Common.DTOs;
using EMS.Application.Features.Leave.Commands;
using EMS.Application.Features.Leave.DTOs;
using EMS.Application.Features.Leave.Queries;
using EMS.Domain.Entities;
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
    [ApiController]
    [Route("api/v1/leave")]
    [Authorize]
    [Produces("application/json")]
    public class LeaveController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LeaveController(IMediator mediator) => _mediator = mediator;

        // ─── Leave Requests ────────────────────────────────────────────────────────

        /// <summary>List leave requests with filtering and pagination. Employee-role callers are always scoped to their own requests.</summary>
        [HttpGet("requests")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<LeaveRequestDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetLeavesQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsPrivilegedLeaveRole();
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<LeaveRequestDto>>.Success(result));
        }

        /// <summary>Get a single leave request by ID. Employee-role callers may only fetch their own request.</summary>
        [HttpGet("requests/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var query = new GetLeaveByIdQuery
            {
                Id = id,
                RequestingUserId = GetCurrentUserId(),
                IsPrivileged = IsPrivilegedLeaveRole()
            };
            var result = await _mediator.Send(query, ct);
            if (result == null) return NotFound();
            return Ok(ApiResponse<LeaveRequestDto>.Success(LeaveRequestDto.FromEntity(result)));
        }

        /// <summary>Apply for leave. Employee-role callers may only apply on their own behalf.</summary>
        [HttpPost("requests")]
        [ProducesResponseType(typeof(ApiResponse<LeaveRequestDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsPrivilegedLeaveRole();
            var result = await _mediator.Send(cmd, ct);
            var dto = LeaveRequestDto.FromEntity(result);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id },
                ApiResponse<LeaveRequestDto>.Success(dto, "Leave request submitted successfully."));
        }

        /// <summary>Update a pending leave request. Employee-role callers may only update their own request.</summary>
        [HttpPut("requests/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveRequestCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsPrivilegedLeaveRole();
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Approve a pending leave request.</summary>
        [HttpPost("requests/{id:guid}/approve")]
        [Authorize(Policy = "CanApproveLeave")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(Guid id, [FromBody] DecisionRequest body, CancellationToken ct)
        {
            await _mediator.Send(new ApproveLeaveCommand
            {
                Id = id,
                ApproverId = GetCurrentUserId(),
                Comments = body?.Comments
            }, ct);
            return NoContent();
        }

        /// <summary>Reject a pending leave request.</summary>
        [HttpPost("requests/{id:guid}/reject")]
        [Authorize(Policy = "CanApproveLeave")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] DecisionRequest body, CancellationToken ct)
        {
            await _mediator.Send(new RejectLeaveCommand
            {
                Id = id,
                ApproverId = GetCurrentUserId(),
                Comments = body?.Comments
            }, ct);
            return NoContent();
        }

        /// <summary>Cancel a pending leave request. Employee-role callers may only cancel their own request.</summary>
        [HttpPost("requests/{id:guid}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new CancelLeaveRequestCommand
            {
                Id = id,
                RequestedByUserId = GetCurrentUserId(),
                IsPrivileged = IsPrivilegedLeaveRole()
            }, ct);
            return NoContent();
        }

        // ─── Leave Balances ────────────────────────────────────────────────────────

        /// <summary>Get leave balances for an employee. Employee-role callers may only fetch their own balances.</summary>
        [HttpGet("balances")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaveBalanceDto>>), 200)]
        public async Task<IActionResult> GetBalances([FromQuery] Guid employeeId, CancellationToken ct)
        {
            var query = new GetLeaveBalancesQuery
            {
                EmployeeId = employeeId,
                RequestingUserId = GetCurrentUserId(),
                IsPrivileged = IsPrivilegedLeaveRole()
            };
            var result = await _mediator.Send(query, ct);
            var dtos = System.Linq.Enumerable.Select(result, LeaveBalanceDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<LeaveBalanceDto>>.Success(dtos));
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

        /// <summary>Admin/HR/Manager may act on any employee's leave requests; everyone else is scoped to their own.</summary>
        private bool IsPrivilegedLeaveRole() =>
            User.IsInRole("Admin") || User.IsInRole("HR") || User.IsInRole("Manager");
    }

    /// <summary>Body for approve / reject decisions.</summary>
    public class DecisionRequest
    {
        public string? Comments { get; set; }
    }
}

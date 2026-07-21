using EMS.Application.Common.DTOs;
using EMS.Application.Features.Attendance.Commands;
using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Features.Attendance.Queries;
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
    [Route("api/v1/attendance")]
    [Authorize]
    [Produces("application/json")]
    public class AttendanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AttendanceController(IMediator mediator) => _mediator = mediator;

        // ─── Check-in / Check-out ──────────────────────────────────────────────────

        /// <summary>Check in. Employee-role callers may only check in for themselves; Admin/HR may record on behalf of any employee.</summary>
        [HttpPost("check-in")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            var result = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<AttendanceRecordDto>.Success(AttendanceRecordDto.FromEntity(result), "Checked in successfully."));
        }

        /// <summary>Check out. Employee-role callers may only check out for themselves; Admin/HR may record on behalf of any employee.</summary>
        [HttpPost("check-out")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            var result = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<AttendanceRecordDto>.Success(AttendanceRecordDto.FromEntity(result), "Checked out successfully."));
        }

        // ─── Attendance records ─────────────────────────────────────────────────────

        /// <summary>List attendance records. Employee-role callers are scoped to their own records; Managers to their team.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AttendanceRecordDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetAttendanceRecordsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsAdminOrHr = IsAdminOrHr();
            query.IsManager = User.IsInRole("Manager");
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<AttendanceRecordDto>>.Success(result));
        }

        /// <summary>Get a single attendance record. Admin/HR, Manager for their team, or the employee themselves.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAttendanceRecordByIdQuery
            {
                Id = id,
                RequestingUserId = GetCurrentUserId(),
                IsAdminOrHr = IsAdminOrHr(),
                IsManager = User.IsInRole("Manager")
            }, ct);
            if (result == null) return NotFound();
            return Ok(ApiResponse<AttendanceRecordDto>.Success(result));
        }

        /// <summary>Create a manual attendance record.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageAttendanceRecords")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateAttendanceRecordCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = AttendanceRecordDto.FromEntity(created);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id },
                ApiResponse<AttendanceRecordDto>.Success(dto, "Attendance record created successfully."));
        }

        /// <summary>Manually correct an attendance record.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageAttendanceRecords")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceRecordDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceRecordCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<AttendanceRecordDto>.Success(AttendanceRecordDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete an attendance record.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageAttendanceRecords")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteAttendanceRecordCommand { Id = id }, ct);
            return NoContent();
        }

        // ─── Attendance corrections ─────────────────────────────────────────────────

        /// <summary>List correction requests. Access is Admin, HR, Manager (scoped to their team).</summary>
        [HttpGet("corrections")]
        [Authorize(Policy = "CanReviewAttendanceCorrections")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AttendanceCorrectionDto>>), 200)]
        public async Task<IActionResult> GetCorrections([FromQuery] GetAttendanceCorrectionsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsAdminOrHr = IsAdminOrHr();
            query.IsManager = User.IsInRole("Manager");
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<AttendanceCorrectionDto>>.Success(result));
        }

        /// <summary>Get a single correction request. Admin/HR, Manager for their team, or the requesting employee.</summary>
        [HttpGet("corrections/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceCorrectionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCorrectionById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAttendanceCorrectionByIdQuery
            {
                Id = id,
                RequestingUserId = GetCurrentUserId(),
                IsAdminOrHr = IsAdminOrHr(),
                IsManager = User.IsInRole("Manager")
            }, ct);
            if (result == null) return NotFound();
            return Ok(ApiResponse<AttendanceCorrectionDto>.Success(result));
        }

        /// <summary>Request a correction to one of the caller's own attendance records.</summary>
        [HttpPost("corrections")]
        [ProducesResponseType(typeof(ApiResponse<AttendanceCorrectionDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> RequestCorrection([FromBody] CreateAttendanceCorrectionCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            var created = await _mediator.Send(cmd, ct);
            var dto = AttendanceCorrectionDto.FromEntity(created);
            return CreatedAtAction(nameof(GetCorrectionById), new { id = dto.Id },
                ApiResponse<AttendanceCorrectionDto>.Success(dto, "Attendance correction requested successfully."));
        }

        /// <summary>Approve a pending correction request.</summary>
        [HttpPost("corrections/{id:guid}/approve")]
        [Authorize(Policy = "CanReviewAttendanceCorrections")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ApproveCorrection(Guid id, [FromBody] DecisionRequest body, CancellationToken ct)
        {
            await _mediator.Send(new ApproveAttendanceCorrectionCommand
            {
                Id = id,
                ApproverUserId = GetCurrentUserId(),
                Comments = body?.Comments
            }, ct);
            return NoContent();
        }

        /// <summary>Reject a pending correction request.</summary>
        [HttpPost("corrections/{id:guid}/reject")]
        [Authorize(Policy = "CanReviewAttendanceCorrections")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RejectCorrection(Guid id, [FromBody] DecisionRequest body, CancellationToken ct)
        {
            await _mediator.Send(new RejectAttendanceCorrectionCommand
            {
                Id = id,
                ApproverUserId = GetCurrentUserId(),
                Comments = body?.Comments
            }, ct);
            return NoContent();
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

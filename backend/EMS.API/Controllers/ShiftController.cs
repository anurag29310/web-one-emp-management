using EMS.Application.Common.DTOs;
using EMS.Application.Features.Attendance.Commands;
using EMS.Application.Features.Attendance.DTOs;
using EMS.Application.Features.Attendance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/shifts")]
    [Authorize]
    [Produces("application/json")]
    public class ShiftController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ShiftController(IMediator mediator) => _mediator = mediator;

        // ─── Shifts ─────────────────────────────────────────────────────────────────

        /// <summary>List shifts.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ShiftDto>>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var shifts = await _mediator.Send(new GetShiftsQuery(), ct);
            var dtos = shifts.Select(ShiftDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<ShiftDto>>.Success(dtos));
        }

        /// <summary>Get a single shift by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ShiftDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var shift = await _mediator.Send(new GetShiftByIdQuery { Id = id }, ct);
            if (shift == null) return NotFound();
            return Ok(ApiResponse<ShiftDto>.Success(ShiftDto.FromEntity(shift)));
        }

        /// <summary>Create a new shift.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageShifts")]
        [ProducesResponseType(typeof(ApiResponse<ShiftDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> Create([FromBody] CreateShiftCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = ShiftDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<ShiftDto>.Success(dto, "Shift created successfully."));
        }

        /// <summary>Update an existing shift.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageShifts")]
        [ProducesResponseType(typeof(ApiResponse<ShiftDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShiftCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<ShiftDto>.Success(ShiftDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a shift.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageShifts")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteShiftCommand { Id = id }, ct);
            return NoContent();
        }

        // ─── Employee shift assignments ─────────────────────────────────────────────

        /// <summary>List an employee's shift assignments. Admin/HR, Manager for their team, or the employee themselves.</summary>
        [HttpGet("~/api/v1/employees/{employeeId:guid}/shifts")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeShiftDto>>), 200)]
        public async Task<IActionResult> GetEmployeeShifts(Guid employeeId, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEmployeeShiftsQuery
            {
                EmployeeId = employeeId,
                RequestingUserId = GetCurrentUserId(),
                IsAdminOrHr = IsAdminOrHr(),
                IsManager = User.IsInRole("Manager")
            }, ct);
            return Ok(ApiResponse<IEnumerable<EmployeeShiftDto>>.Success(result));
        }

        /// <summary>Assign a shift to an employee.</summary>
        [HttpPost("~/api/v1/employees/{employeeId:guid}/shifts")]
        [Authorize(Policy = "CanManageShifts")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeShiftDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> AssignEmployeeShift(Guid employeeId, [FromBody] AssignEmployeeShiftCommand cmd, CancellationToken ct)
        {
            cmd.EmployeeId = employeeId;
            var created = await _mediator.Send(cmd, ct);
            var dto = EmployeeShiftDto.FromEntity(created);
            return CreatedAtAction(nameof(GetEmployeeShifts), new { employeeId }, ApiResponse<EmployeeShiftDto>.Success(dto, "Shift assigned successfully."));
        }

        /// <summary>Update an employee's shift assignment.</summary>
        [HttpPut("~/api/v1/employees/{employeeId:guid}/shifts/{assignmentId:guid}")]
        [Authorize(Policy = "CanManageShifts")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeShiftDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateEmployeeShift(Guid employeeId, Guid assignmentId, [FromBody] UpdateEmployeeShiftCommand cmd, CancellationToken ct)
        {
            cmd.EmployeeId = employeeId;
            cmd.AssignmentId = assignmentId;
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<EmployeeShiftDto>.Success(EmployeeShiftDto.FromEntity(updated)));
        }

        /// <summary>End (soft-delete) an employee's shift assignment.</summary>
        [HttpDelete("~/api/v1/employees/{employeeId:guid}/shifts/{assignmentId:guid}")]
        [Authorize(Policy = "CanManageShifts")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> EndEmployeeShift(Guid employeeId, Guid assignmentId, CancellationToken ct)
        {
            await _mediator.Send(new EndEmployeeShiftCommand { EmployeeId = employeeId, AssignmentId = assignmentId }, ct);
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

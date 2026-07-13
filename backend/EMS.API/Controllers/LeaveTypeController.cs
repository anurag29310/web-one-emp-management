using EMS.Application.Features.Leave.Commands;
using EMS.Application.Features.Leave.DTOs;
using EMS.Application.Features.Leave.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/leave-types")]
    [Authorize]
    [Produces("application/json")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LeaveTypeController(IMediator mediator) => _mediator = mediator;

        /// <summary>List leave types.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<LeaveTypeDto>>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var leaveTypes = await _mediator.Send(new GetLeaveTypesQuery(), ct);
            var dtos = leaveTypes.Select(LeaveTypeDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<LeaveTypeDto>>.Success(dtos));
        }

        /// <summary>Get a single leave type by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<LeaveTypeDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var leaveType = await _mediator.Send(new GetLeaveTypeByIdQuery { Id = id }, ct);
            if (leaveType == null) return NotFound();
            return Ok(ApiResponse<LeaveTypeDto>.Success(LeaveTypeDto.FromEntity(leaveType)));
        }

        /// <summary>Create a new leave type.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageLeaveTypes")]
        [ProducesResponseType(typeof(ApiResponse<LeaveTypeDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateLeaveTypeCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = LeaveTypeDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<LeaveTypeDto>.Success(dto, "Leave type created successfully."));
        }

        /// <summary>Update an existing leave type.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageLeaveTypes")]
        [ProducesResponseType(typeof(ApiResponse<LeaveTypeDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeaveTypeCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<LeaveTypeDto>.Success(LeaveTypeDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a leave type.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageLeaveTypes")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteLeaveTypeCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

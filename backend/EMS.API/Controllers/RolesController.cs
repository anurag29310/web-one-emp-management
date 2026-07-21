using EMS.Application.Features.Roles.Commands;
using EMS.Application.Features.Roles.DTOs;
using EMS.Application.Features.Roles.Queries;
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
    [Route("api/v1/roles")]
    [Authorize(Policy = "CanManageUsers")]
    [Produces("application/json")]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator) => _mediator = mediator;

        /// <summary>List roles.</summary>
        [HttpGet]
        [Authorize(Policy = "CanViewRoles")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            var roles = await _mediator.Send(new GetRolesQuery { IncludeDeleted = includeDeleted }, ct);
            var dtos = roles.Select(RoleDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<RoleDto>>.Success(dtos));
        }

        /// <summary>Get a single role by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var role = await _mediator.Send(new GetRoleByIdQuery { Id = id }, ct);
            if (role == null) return NotFound();
            return Ok(ApiResponse<RoleDto>.Success(RoleDto.FromEntity(role)));
        }

        /// <summary>Create a new role.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateRoleCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = RoleDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<RoleDto>.Success(dto, "Role created successfully."));
        }

        /// <summary>Update an existing role.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<RoleDto>.Success(RoleDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a role. Fails if any active user is still assigned to it.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteRoleCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted role.</summary>
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreRoleCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

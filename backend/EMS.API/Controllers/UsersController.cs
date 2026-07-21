using EMS.Application.Features.Users.Commands;
using EMS.Application.Features.Users.DTOs;
using EMS.Application.Features.Users.Queries;
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
    [Route("api/v1/users")]
    [Authorize(Policy = "CanManageUsers")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator) => _mediator = mediator;

        /// <summary>List user accounts.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeDeleted = false,
            [FromQuery] Guid? roleId = null,
            [FromQuery] bool? isActive = null,
            CancellationToken ct = default)
        {
            var users = await _mediator.Send(new GetUsersQuery { IncludeDeleted = includeDeleted, RoleId = roleId, IsActive = isActive }, ct);
            var dtos = users.Select(UserDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<UserDto>>.Success(dtos));
        }

        /// <summary>Get a single user account by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var user = await _mediator.Send(new GetUserByIdQuery { Id = id }, ct);
            if (user == null) return NotFound();
            return Ok(ApiResponse<UserDto>.Success(UserDto.FromEntity(user)));
        }

        /// <summary>Create a new user account.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateUserCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = UserDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<UserDto>.Success(dto, "User created successfully."));
        }

        /// <summary>Update an existing user account.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<UserDto>.Success(UserDto.FromEntity(updated)));
        }

        /// <summary>Activate or deactivate a user account.</summary>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateUserStatusCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<UserDto>.Success(UserDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a user account.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteUserCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted user account.</summary>
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreUserCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

using EMS.Application.Common.DTOs;
using EMS.Application.Features.Clients;
using EMS.Application.Features.Clients.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>
    /// Client Master. Read access is open to any authenticated user (Employees see clients
    /// linked to their assigned tasks once Task Management ships); all mutations are Admin-only
    /// per requirements.md — Client Master is deliberately not delegated to HR.
    /// </summary>
    [ApiController]
    [Route("api/v1/clients")]
    [Authorize]
    [Produces("application/json")]
    [EnableRateLimiting("WriteActionPolicy")]
    public class ClientController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClientController(IMediator mediator) => _mediator = mediator;

        /// <summary>List clients with search, active-status filter, and pagination.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetClientsQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<ClientDto>>.Success(result));
        }

        /// <summary>Get a single client by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var client = await _mediator.Send(new GetClientByIdQuery { Id = id }, ct);
            if (client == null) return NotFound();
            return Ok(ApiResponse<ClientDto>.Success(ClientDto.FromEntity(client)));
        }

        /// <summary>Create a new client.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateClientCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = ClientDto.FromEntity(created);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id },
                ApiResponse<ClientDto>.Success(dto, "Client created successfully."));
        }

        /// <summary>Update an existing client.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<ClientDto>.Success(ClientDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a client.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteClientCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Activate a client (eligible to receive new tasks).</summary>
        [HttpPost("{id:guid}/activate")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ActivateClientCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Deactivate a client (blocks new tasks; existing history is retained).</summary>
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeactivateClientCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Archive a client, retiring it from active workflows while retaining history.</summary>
        [HttpPost("{id:guid}/archive")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ArchiveClientCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted or archived client.</summary>
        [HttpPost("{id:guid}/restore")]
        [Authorize(Policy = "CanManageClients")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreClientCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

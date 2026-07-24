using EMS.Application.Features.OfficeLocations;
using EMS.Application.Features.OfficeLocations.DTOs;
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
    [Route("api/v1/office-locations")]
    [Authorize]
    [Produces("application/json")]
    public class OfficeLocationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OfficeLocationController(IMediator mediator) => _mediator = mediator;

        /// <summary>List all active office locations.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OfficeLocationDto>>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var locations = await _mediator.Send(new GetOfficeLocationsQuery(), ct);
            var dtos = locations.Select(OfficeLocationDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<OfficeLocationDto>>.Success(dtos));
        }

        /// <summary>Get a single office location by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<OfficeLocationDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var location = await _mediator.Send(new GetOfficeLocationByIdQuery { Id = id }, ct);
            if (location == null) return NotFound();
            return Ok(ApiResponse<OfficeLocationDto>.Success(OfficeLocationDto.FromEntity(location)));
        }

        /// <summary>Create a new office location.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<OfficeLocationDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateOfficeLocationCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = OfficeLocationDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<OfficeLocationDto>.Success(dto, "Office location created successfully."));
        }

        /// <summary>Update an existing office location.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<OfficeLocationDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOfficeLocationCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<OfficeLocationDto>.Success(OfficeLocationDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete an office location.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteOfficeLocationCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

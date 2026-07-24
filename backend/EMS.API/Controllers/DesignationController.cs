using EMS.Application.Features.Designations;
using EMS.Application.Features.Designations.DTOs;
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
    [Route("api/v1/designations")]
    [Authorize]
    [Produces("application/json")]
    public class DesignationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DesignationController(IMediator mediator) => _mediator = mediator;

        /// <summary>List all active designations.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DesignationDto>>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var designations = await _mediator.Send(new GetDesignationsQuery(), ct);
            var dtos = designations.Select(DesignationDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<DesignationDto>>.Success(dtos));
        }

        /// <summary>Get a single designation by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<DesignationDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var designation = await _mediator.Send(new GetDesignationByIdQuery { Id = id }, ct);
            if (designation == null) return NotFound();
            return Ok(ApiResponse<DesignationDto>.Success(DesignationDto.FromEntity(designation)));
        }

        /// <summary>Create a new designation.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<DesignationDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateDesignationCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = DesignationDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<DesignationDto>.Success(dto, "Designation created successfully."));
        }

        /// <summary>Update an existing designation.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<DesignationDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDesignationCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<DesignationDto>.Success(DesignationDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a designation.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteDesignationCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

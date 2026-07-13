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
    [Route("api/v1/holidays")]
    [Authorize]
    [Produces("application/json")]
    public class HolidayController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HolidayController(IMediator mediator) => _mediator = mediator;

        /// <summary>List holidays, optionally filtered by office location, year, and optionality.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<HolidayDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetHolidaysQuery query, CancellationToken ct)
        {
            var holidays = await _mediator.Send(query, ct);
            var dtos = holidays.Select(HolidayDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<HolidayDto>>.Success(dtos));
        }

        /// <summary>Get a single holiday by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<HolidayDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var holiday = await _mediator.Send(new GetHolidayByIdQuery { Id = id }, ct);
            if (holiday == null) return NotFound();
            return Ok(ApiResponse<HolidayDto>.Success(HolidayDto.FromEntity(holiday)));
        }

        /// <summary>Create a new holiday.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageHolidays")]
        [ProducesResponseType(typeof(ApiResponse<HolidayDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> Create([FromBody] CreateHolidayCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = HolidayDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<HolidayDto>.Success(dto, "Holiday created successfully."));
        }

        /// <summary>Update an existing holiday.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageHolidays")]
        [ProducesResponseType(typeof(ApiResponse<HolidayDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHolidayCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<HolidayDto>.Success(HolidayDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a holiday.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageHolidays")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteHolidayCommand { Id = id }, ct);
            return NoContent();
        }
    }
}

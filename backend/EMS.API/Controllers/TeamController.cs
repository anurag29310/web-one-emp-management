using EMS.Application.Features.Employees.DTOs;
using EMS.Application.Features.Employees.Queries;
using EMS.Application.Features.Teams;
using EMS.Application.Features.Teams.DTOs;
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
    [Route("api/v1/teams")]
    [Authorize]
    [Produces("application/json")]
    public class TeamController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TeamController(IMediator mediator) => _mediator = mediator;

        /// <summary>List all active teams.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TeamDto>>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var teams = await _mediator.Send(new GetTeamsQuery(), ct);
            var dtos = teams.Select(TeamDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<TeamDto>>.Success(dtos));
        }

        /// <summary>Get a single team by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var team = await _mediator.Send(new GetTeamByIdQuery { Id = id }, ct);
            if (team == null) return NotFound();
            return Ok(ApiResponse<TeamDto>.Success(TeamDto.FromEntity(team)));
        }

        /// <summary>Create a new team.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateTeamCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = TeamDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<TeamDto>.Success(dto, "Team created successfully."));
        }

        /// <summary>Update an existing team.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<TeamDto>.Success(TeamDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a team.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteTeamCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>List employees belonging to this team.</summary>
        [HttpGet("{id:guid}/employees")]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), 200)]
        public async Task<IActionResult> GetEmployees(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetEmployeesByTeamQuery { TeamId = id, Page = page, PageSize = pageSize }, ct);
            var dtos = result.Select(EmployeeDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Success(dtos));
        }
    }
}

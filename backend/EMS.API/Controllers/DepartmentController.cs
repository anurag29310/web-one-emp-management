using EMS.Application.Features.Departments;
using EMS.Application.Features.Departments.DTOs;
using EMS.Application.Features.Employees.DTOs;
using EMS.Application.Features.Employees.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/departments")]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentController(IMediator mediator) => _mediator = mediator;

        /// <summary>List all active departments.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<DepartmentDto>>), 200)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var depts = await _mediator.Send(new GetDepartmentsQuery(), ct);
            var dtos = depts.Select(DepartmentDto.FromEntity);
            return Ok(ApiResponse<System.Collections.Generic.IEnumerable<DepartmentDto>>.Success(dtos));
        }

        /// <summary>Get a single department by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var dept = await _mediator.Send(new GetDepartmentByIdQuery { Id = id }, ct);
            if (dept == null) return NotFound();
            return Ok(ApiResponse<DepartmentDto>.Success(DepartmentDto.FromEntity(dept)));
        }

        /// <summary>Create a new department.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand cmd, CancellationToken ct)
        {
            var created = await _mediator.Send(cmd, ct);
            var dto = DepartmentDto.FromEntity(created);
            return CreatedAtAction(nameof(Get), new { id = dto.Id },
                ApiResponse<DepartmentDto>.Success(dto, "Department created successfully."));
        }

        /// <summary>Update an existing department.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<DepartmentDto>.Success(DepartmentDto.FromEntity(updated)));
        }

        /// <summary>Soft-delete a department.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteDepartmentCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted department.</summary>
        [HttpPost("{id:guid}/restore")]
        [Authorize(Policy = "CanManageDepartments")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreDepartmentCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>List employees belonging to this department.</summary>
        [HttpGet("{id:guid}/employees")]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<EmployeeDto>>), 200)]
        public async Task<IActionResult> GetEmployees(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetEmployeesByDepartmentQuery { DepartmentId = id, Page = page, PageSize = pageSize }, ct);
            var dtos = result.Select(EmployeeDto.FromEntity);
            return Ok(ApiResponse<System.Collections.Generic.IEnumerable<EmployeeDto>>.Success(dtos));
        }
    }
}

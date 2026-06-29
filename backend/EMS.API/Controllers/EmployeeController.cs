using EMS.Application.Common.DTOs;
using EMS.Application.Features.Employees.Commands;
using EMS.Application.Features.Employees.DTOs;
using EMS.Application.Features.Employees.Queries;
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
    [Route("api/v1/employees")]
    [Authorize]
    [Produces("application/json")]
    public class EmployeeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeeController(IMediator mediator) => _mediator = mediator;

        /// <summary>List employees with filtering, sorting, and pagination.</summary>
        [HttpGet]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetEmployeesQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<EmployeeDto>>.Success(result));
        }

        /// <summary>Get a single employee by ID.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetEmployeeByIdQuery { Id = id }, ct);
            if (result == null) return NotFound();
            return Ok(ApiResponse<EmployeeDto>.Success(EmployeeDto.FromEntity(result)));
        }

        /// <summary>Create a new employee.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageEmployees")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand cmd, CancellationToken ct)
        {
            var emp = await _mediator.Send(cmd, ct);
            var dto = EmployeeDto.FromEntity(emp);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id },
                ApiResponse<EmployeeDto>.Success(dto, "Employee created successfully."));
        }

        /// <summary>Full update of an employee record.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageEmployees")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            var emp = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<EmployeeDto>.Success(EmployeeDto.FromEntity(emp)));
        }

        /// <summary>Self-service profile update (phone, address, emergency contact).</summary>
        [HttpPatch("{id:guid}/profile")]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateEmployeeProfileCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Update employment status (Active, Inactive, Terminated, OnLeave).</summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "CanManageEmployees")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateEmployeeStatusCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Soft-delete an employee.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageEmployees")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteEmployeeCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted employee.</summary>
        [HttpPost("{id:guid}/restore")]
        [Authorize(Policy = "CanManageEmployees")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreEmployeeCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Get the manager chain above this employee (reporting hierarchy upward).</summary>
        [HttpGet("{id:guid}/reporting-hierarchy")]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReportingHierarchy(Guid id, CancellationToken ct)
        {
            var chain = await _mediator.Send(new GetReportingHierarchyQuery { EmployeeId = id }, ct);
            var dtos = chain.Select(EmployeeDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Success(dtos));
        }

        /// <summary>Get employees who report directly to this employee.</summary>
        [HttpGet("{id:guid}/direct-reports")]
        [Authorize(Policy = "CanViewEmployees")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeDto>>), 200)]
        public async Task<IActionResult> GetDirectReports(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetDirectReportsQuery { ManagerId = id, Page = page, PageSize = pageSize }, ct);
            var dtos = result.Select(EmployeeDto.FromEntity);
            return Ok(ApiResponse<IEnumerable<EmployeeDto>>.Success(dtos));
        }
    }
}

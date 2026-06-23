using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "CanViewEmployees")]
        public async Task<IActionResult> Get([FromQuery] EMS.Application.Features.Employees.Queries.GetEmployeesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(new { data = result });
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "CanViewEmployees")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new EMS.Application.Features.Employees.Queries.GetEmployeeByIdQuery { Id = id });
            if (result == null) return NotFound();
            return Ok(new { data = result });
        }

        [HttpGet("department/{departmentId}")]
        [Authorize(Policy = "CanViewEmployees")]
        public async Task<IActionResult> GetByDepartment(Guid departmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new EMS.Application.Features.Employees.Queries.GetEmployeesByDepartmentQuery { DepartmentId = departmentId, Page = page, PageSize = pageSize });
            return Ok(new { data = result });
        }

        [HttpGet("reporting/{managerId}")]
        [Authorize(Policy = "CanViewEmployees")]
        public async Task<IActionResult> GetReporting(Guid managerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new EMS.Application.Features.Employees.Queries.GetReportingEmployeesQuery { ManagerId = managerId, Page = page, PageSize = pageSize });
            return Ok(new { data = result });
        }

        [HttpPost]
        [Authorize(Policy = "CanManageEmployees")]
        public async Task<IActionResult> Create([FromBody] EMS.Application.Features.Employees.Commands.CreateEmployeeCommand cmd)
        {
            var emp = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetById), new { id = emp.Id }, new { data = emp });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageEmployees")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EMS.Application.Features.Employees.Commands.UpdateEmployeeCommand cmd)
        {
            if (id != cmd.Id) return BadRequest();
            var emp = await _mediator.Send(cmd);
            return Ok(new { data = emp });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanManageEmployees")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new EMS.Application.Features.Employees.Commands.DeleteEmployeeCommand { Id = id });
            return NoContent();
        }

        [HttpPatch("{id}/activate")]
        [Authorize(Policy = "CanManageEmployees")]
        public async Task<IActionResult> Activate(Guid id)
        {
            await _mediator.Send(new EMS.Application.Features.Employees.Commands.ActivateEmployeeCommand { Id = id });
            return NoContent();
        }

        [HttpPatch("{id}/deactivate")]
        [Authorize(Policy = "CanManageEmployees")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            await _mediator.Send(new EMS.Application.Features.Employees.Commands.DeactivateEmployeeCommand { Id = id });
            return NoContent();
        }
    }
}

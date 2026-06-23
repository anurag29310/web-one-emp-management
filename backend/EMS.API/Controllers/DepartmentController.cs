using EMS.Application.Features.Departments;
using EMS.Application.Features.Departments.Handlers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly CreateDepartmentCommandHandler _createHandler;
        private readonly UpdateDepartmentCommandHandler _updateHandler;
        private readonly DeleteDepartmentCommandHandler _deleteHandler;
        private readonly GetDepartmentsQueryHandler _listHandler;
        private readonly GetDepartmentByIdQueryHandler _getHandler;

        public DepartmentController(
            CreateDepartmentCommandHandler createHandler,
            UpdateDepartmentCommandHandler updateHandler,
            DeleteDepartmentCommandHandler deleteHandler,
            GetDepartmentsQueryHandler listHandler,
            GetDepartmentByIdQueryHandler getHandler)
        {
            _createHandler = createHandler;
            _updateHandler = updateHandler;
            _deleteHandler = deleteHandler;
            _listHandler = listHandler;
            _getHandler = getHandler;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var depts = await _listHandler.Handle(new GetDepartmentsQuery());
            return Ok(new { data = depts });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var dept = await _getHandler.Handle(new GetDepartmentByIdQuery { Id = id });
            if (dept == null) return NotFound();
            return Ok(new { data = dept });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand cmd)
        {
            var created = await _createHandler.Handle(cmd);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { data = created });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentCommand cmd)
        {
            if (id != cmd.Id) return BadRequest();
            var updated = await _updateHandler.Handle(cmd);
            return Ok(new { data = updated });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _deleteHandler.Handle(new DeleteDepartmentCommand { Id = id });
            return NoContent();
        }
    }
}

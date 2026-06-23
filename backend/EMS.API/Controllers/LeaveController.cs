using EMS.Application.Features.Leave.Commands;
using EMS.Application.Features.Leave.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController : ControllerBase
    {
        private readonly IMediator _mediator;

        public LeaveController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(CreateLeaveRequestCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get([FromQuery] GetLeavesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("balances/{employeeId}")]
        [Authorize]
        public async Task<IActionResult> GetBalances(int employeeId)
        {
            var result = await _mediator.Send(new GetLeaveBalancesQuery { EmployeeId = Guid.Parse(employeeId.ToString()) });
            return Ok(result);
        }

        [HttpGet("holidays")]
        [Authorize]
        public async Task<IActionResult> GetHolidays([FromQuery] GetHolidaysQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetLeaveByIdQuery { Id =Guid.Parse(id.ToString()) });
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Policy = "CanManageLeaves")]
        public async Task<IActionResult> Approve(int id)
        {
            await _mediator.Send(new ApproveLeaveCommand { Id = Guid.Parse(id.ToString()) });
            return NoContent();
        }

        [HttpPost("{id}/reject")]
        [Authorize(Policy = "CanManageLeaves")]
        public async Task<IActionResult> Reject(int id)
        {
            await _mediator.Send(new RejectLeaveCommand { Id = Guid.Parse(id.ToString()) });
            return NoContent();
        }
    }
}

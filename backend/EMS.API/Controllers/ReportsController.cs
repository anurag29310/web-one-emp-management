using EMS.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("employees")]
        [Authorize]
        public async Task<IActionResult> EmployeeReport()
        {
            var result = await _mediator.Send(new GetEmployeeReportQuery());
            return Ok(result);
        }

        [HttpGet("departments")]
        [Authorize]
        public async Task<IActionResult> DepartmentCounts()
        {
            var result = await _mediator.Send(new GetDepartmentCountsQuery());
            return Ok(result);
        }

        [HttpGet("leave-summary")]
        [Authorize]
        public async Task<IActionResult> LeaveSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var q = new GetLeaveSummaryQuery { From = from, To = to };
            var result = await _mediator.Send(q);
            return Ok(result);
        }
    }
}

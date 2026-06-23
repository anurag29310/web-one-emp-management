using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Features.Payroll.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayrollController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PayrollController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("process")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Process([FromBody] ProcessPayrollCommand cmd)
        {
            var id = await _mediator.Send(cmd);
            return Accepted(new { payrollRunId = id });
        }

        [HttpGet("payslips/{payslipId}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadPayslip(Guid payslipId)
        {
            var result = await _mediator.Send(new DownloadPayslipQuery { PayslipId = payslipId });
            return File(result.Content, result.ContentType, result.FileName);
        }

            [HttpGet("salary-structures")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> GetSalaryStructures()
            {
                var list = await _mediator.Send(new EMS.Application.Features.Payroll.Queries.GetSalaryStructuresQuery());
                return Ok(new { data = list });
            }

            [HttpGet("salary-structures/{id}")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> GetSalaryStructure(Guid id)
            {
                var s = await _mediator.Send(new EMS.Application.Features.Payroll.Queries.GetSalaryStructureByIdQuery { Id = id });
                if (s == null) return NotFound();
                return Ok(new { data = s });
            }

            [HttpPost("salary-structures")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> CreateSalaryStructure([FromBody] EMS.Application.Features.Payroll.Commands.CreateSalaryStructureCommand cmd)
            {
                var id = await _mediator.Send(cmd);
                return CreatedAtAction(nameof(GetSalaryStructure), new { id }, new { id });
            }

            [HttpPut("salary-structures/{id}")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> UpdateSalaryStructure(Guid id, [FromBody] EMS.Application.Features.Payroll.Commands.UpdateSalaryStructureCommand cmd)
            {
                cmd.Id = id;
                await _mediator.Send(cmd);
                return NoContent();
            }

            [HttpDelete("salary-structures/{id}")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> DeleteSalaryStructure(Guid id)
            {
                await _mediator.Send(new EMS.Application.Features.Payroll.Commands.DeleteSalaryStructureCommand { Id = id });
                return NoContent();
            }

            [HttpPost("dry-run")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> DryRun([FromBody] EMS.Application.Features.Payroll.Queries.DryRunPayrollQuery cmd)
            {
                var result = await _mediator.Send(cmd);
                return Ok(new { data = result });
            }

            [HttpGet("runs")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> GetRuns()
            {
                var runs = await _mediator.Send(new EMS.Application.Features.Payroll.Queries.GetPayrollRunsQuery());
                return Ok(new { data = runs });
            }

            [HttpGet("runs/{id}")]
            [Authorize(Roles = "Admin,HR")]
            public async Task<IActionResult> GetRun(Guid id)
            {
                var run = await _mediator.Send(new EMS.Application.Features.Payroll.Queries.GetPayrollRunsQuery());
                var found = run.FirstOrDefault(r => r.Id == id);
                if (found == null) return NotFound();
                return Ok(new { data = found });
            }

            [HttpPost("runs/{id}/approve")]
            [Authorize(Roles = "Admin")]
            public async Task<IActionResult> ApproveRun(Guid id, [FromBody] EMS.Application.Features.Payroll.Commands.ApprovePayrollRunCommand cmd)
            {
                cmd.PayrollRunId = id;
                await _mediator.Send(cmd);
                return NoContent();
            }
    }
}

using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Features.Payroll.Dtos;
using EMS.Application.Features.Payroll.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/v1/payroll")]
    [Authorize]
    [Produces("application/json")]
    public class PayrollController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PayrollController(IMediator mediator) => _mediator = mediator;

        // ─── Payroll Runs ──────────────────────────────────────────────────────────

        /// <summary>Trigger payroll processing for a period. Generates payslips and PDFs for all active employees.</summary>
        [HttpPost("process")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<object>), 202)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> Process([FromBody] ProcessPayrollCommand cmd, CancellationToken ct)
        {
            cmd.ProcessedBy = GetCurrentUserId();
            var id = await _mediator.Send(cmd, ct);
            return Accepted(ApiResponse<object>.Success(new { payrollRunId = id }, "Payroll processing started."));
        }

        /// <summary>Preview payslip calculations for a period without persisting anything.</summary>
        [HttpPost("dry-run")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PayslipPreview>>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> DryRun([FromBody] DryRunPayrollQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<IEnumerable<PayslipPreview>>.Success(result));
        }

        /// <summary>List all payroll runs.</summary>
        [HttpGet("runs")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PayrollRunDto>>), 200)]
        public async Task<IActionResult> GetRuns(CancellationToken ct)
        {
            var runs = await _mediator.Send(new GetPayrollRunsQuery(), ct);
            return Ok(ApiResponse<IEnumerable<PayrollRunDto>>.Success(runs));
        }

        /// <summary>Get a single payroll run by ID, including its payslips.</summary>
        [HttpGet("runs/{id:guid}")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<PayrollRunDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetRun(Guid id, CancellationToken ct)
        {
            var run = await _mediator.Send(new GetPayrollRunByIdQuery { Id = id }, ct);
            if (run == null) return NotFound();
            return Ok(ApiResponse<PayrollRunDto>.Success(run));
        }

        /// <summary>Approve a completed payroll run. The approver is always the authenticated caller.</summary>
        [HttpPost("runs/{id:guid}/approve")]
        [Authorize(Policy = "CanApprovePayroll")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> ApproveRun(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ApprovePayrollRunCommand { PayrollRunId = id, ApprovedBy = GetCurrentUserId() }, ct);
            return NoContent();
        }

        // ─── Salary Structures ─────────────────────────────────────────────────────

        /// <summary>List all salary structures.</summary>
        [HttpGet("salary-structures")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<SalaryStructureDto>>), 200)]
        public async Task<IActionResult> GetSalaryStructures(CancellationToken ct)
        {
            var list = await _mediator.Send(new GetSalaryStructuresQuery(), ct);
            return Ok(ApiResponse<IEnumerable<SalaryStructureDto>>.Success(list));
        }

        /// <summary>Get a single salary structure by ID.</summary>
        [HttpGet("salary-structures/{id:guid}")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<SalaryStructureDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSalaryStructure(Guid id, CancellationToken ct)
        {
            var structure = await _mediator.Send(new GetSalaryStructureByIdQuery { Id = id }, ct);
            if (structure == null) return NotFound();
            return Ok(ApiResponse<SalaryStructureDto>.Success(structure));
        }

        /// <summary>Create a new salary structure for an employee.</summary>
        [HttpPost("salary-structures")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> CreateSalaryStructure([FromBody] CreateSalaryStructureCommand cmd, CancellationToken ct)
        {
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetSalaryStructure), new { id },
                ApiResponse<object>.Success(new { id }, "Salary structure created successfully."));
        }

        /// <summary>Update an existing salary structure.</summary>
        [HttpPut("salary-structures/{id:guid}")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateSalaryStructure(Guid id, [FromBody] UpdateSalaryStructureCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Delete a salary structure.</summary>
        [HttpDelete("salary-structures/{id:guid}")]
        [Authorize(Policy = "CanManagePayroll")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteSalaryStructure(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteSalaryStructureCommand { Id = id }, ct);
            return NoContent();
        }

        // ─── Payslips ──────────────────────────────────────────────────────────────

        /// <summary>List payslips for an employee. Employee-role callers are always scoped to their own payslips.</summary>
        [HttpGet("payslips")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PayslipDto>>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<IActionResult> GetPayslips([FromQuery] Guid? employeeId, CancellationToken ct)
        {
            var query = new GetPayslipsForEmployeeQuery
            {
                EmployeeId = employeeId,
                RequestingUserId = GetCurrentUserId(),
                IsPrivileged = IsPrivilegedPayrollRole()
            };
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<IEnumerable<PayslipDto>>.Success(result));
        }

        /// <summary>Download a payslip PDF. Employee-role callers may only download their own payslip.</summary>
        [HttpGet("payslips/{payslipId:guid}/download")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadPayslip(Guid payslipId, CancellationToken ct)
        {
            var query = new DownloadPayslipQuery
            {
                PayslipId = payslipId,
                RequestingUserId = GetCurrentUserId(),
                IsPrivileged = IsPrivilegedPayrollRole()
            };
            var result = await _mediator.Send(query, ct);
            return File(result.Content, result.ContentType, result.FileName);
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("User identity could not be resolved.");

            return id;
        }

        /// <summary>Admin/HR may act on or view any employee's payroll data; everyone else is scoped to their own.</summary>
        private bool IsPrivilegedPayrollRole() =>
            User.IsInRole("Admin") || User.IsInRole("HR");
    }
}

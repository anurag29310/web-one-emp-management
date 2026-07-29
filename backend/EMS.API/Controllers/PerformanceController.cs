using EMS.Application.Common.DTOs;
using EMS.Application.Features.Performance.Commands;
using EMS.Application.Features.Performance.DTOs;
using EMS.Application.Features.Performance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>
    /// Performance Management: Goals, KPI Tracking, Performance Reviews, Promotions. Extends the
    /// manager-tier authorization pattern already used by Attendance (IsManager + GetDirectReportIdsAsync)
    /// rather than the Admin/HR-only pattern used by Recruitment/Assets — a Manager may act on their own
    /// direct reports' records, an Employee may act on their own, and Admin/HR see and act on everything.
    /// GET endpoints and self-service actions (progress updates, self-assessment) use the bare [Authorize]
    /// on the class and are scoped inside the handler; actions that create or fully edit a record require
    /// the CanManagePerformance policy (Admin, HR, Manager); promotion approve/reject/delete/restore
    /// require the stricter CanApprovePromotions policy (Admin, HR only — a Manager cannot approve their
    /// own proposal).
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    [Produces("application/json")]
    public class PerformanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PerformanceController(IMediator mediator) => _mediator = mediator;

        // ─── Goals ─────────────────────────────────────────────────────────────────

        /// <summary>List goals — paginated, filterable by employeeId/status/category. Employees see their own; Managers see their own and their reports'; Admin/HR see all.</summary>
        [HttpGet("goals")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PerformanceGoalDto>>), 200)]
        public async Task<IActionResult> GetGoals([FromQuery] GetGoalsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsAdminOrHr();
            query.IsManager = User.IsInRole("Manager");
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<PerformanceGoalDto>>.Success(result));
        }

        /// <summary>Get a single goal by ID.</summary>
        [HttpGet("goals/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PerformanceGoalDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGoalById(Guid id, CancellationToken ct)
        {
            var goal = await _mediator.Send(new GetGoalByIdQuery { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr(), IsManager = User.IsInRole("Manager") }, ct);
            if (goal == null) return NotFound();
            return Ok(ApiResponse<PerformanceGoalDto>.Success(goal));
        }

        /// <summary>Set a goal for an employee. Manager/Admin/HR only; a Manager may only set goals for their own direct reports.</summary>
        [HttpPost("goals")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> CreateGoal([FromBody] CreateGoalCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetGoalById), new { id }, ApiResponse<object>.Success(new { id }, "Goal created successfully."));
        }

        /// <summary>Update a goal's details/status. Manager/Admin/HR only.</summary>
        [HttpPut("goals/{id:guid}")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGoal(Guid id, [FromBody] UpdateGoalCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Update a goal's progress percentage. The goal's own employee, their manager, or Admin/HR.</summary>
        [HttpPost("goals/{id:guid}/progress")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGoalProgress(Guid id, [FromBody] UpdateGoalProgressCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Soft-delete a goal. Manager/Admin/HR only.</summary>
        [HttpDelete("goals/{id:guid}")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteGoal(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteGoalCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr(), IsManager = User.IsInRole("Manager") }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted goal.</summary>
        [HttpPost("goals/{id:guid}/restore")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RestoreGoal(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreGoalCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr(), IsManager = User.IsInRole("Manager") }, ct);
            return NoContent();
        }

        /// <summary>Add a KPI to a goal ("KPI Tracking"). Manager/Admin/HR only.</summary>
        [HttpPost("goals/{id:guid}/kpis")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddGoalKpi(Guid id, [FromBody] AddGoalKpiCommand cmd, CancellationToken ct)
        {
            cmd.GoalId = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            var kpiId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(kpiId, "KPI added."));
        }

        /// <summary>Update a KPI's current value. The goal's own employee, their manager, or Admin/HR.</summary>
        [HttpPost("kpis/{kpiId:guid}/progress")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGoalKpiProgress(Guid kpiId, [FromBody] UpdateGoalKpiProgressCommand cmd, CancellationToken ct)
        {
            cmd.Id = kpiId;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        // ─── Performance Reviews ───────────────────────────────────────────────────

        /// <summary>List reviews — paginated, filterable by employeeId/reviewerEmployeeId/status. Employees see reviews where they're the subject or reviewer; Managers additionally see their reports'; Admin/HR see all.</summary>
        [HttpGet("reviews")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PerformanceReviewDto>>), 200)]
        public async Task<IActionResult> GetReviews([FromQuery] GetReviewsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsAdminOrHr();
            query.IsManager = User.IsInRole("Manager");
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<PerformanceReviewDto>>.Success(result));
        }

        /// <summary>Get a single review by ID.</summary>
        [HttpGet("reviews/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PerformanceReviewDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetReviewById(Guid id, CancellationToken ct)
        {
            var review = await _mediator.Send(new GetReviewByIdQuery { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr(), IsManager = User.IsInRole("Manager") }, ct);
            if (review == null) return NotFound();
            return Ok(ApiResponse<PerformanceReviewDto>.Success(review));
        }

        /// <summary>Start a review cycle for an employee (Draft). Manager/Admin/HR only; a Manager may only start reviews for their own direct reports, and must set themselves as reviewer.</summary>
        [HttpPost("reviews")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetReviewById), new { id }, ApiResponse<object>.Success(new { id }, "Review created successfully."));
        }

        /// <summary>Submit the employee's self-assessment. Draft → SelfAssessmentSubmitted. Only the reviewed employee (or Admin/HR).</summary>
        [HttpPost("reviews/{id:guid}/self-assessment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SubmitSelfAssessment(Guid id, [FromBody] SubmitSelfAssessmentCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Submit the manager's review, completing the cycle. Draft/SelfAssessmentSubmitted → Completed. Only the assigned reviewer (or Admin/HR).</summary>
        [HttpPost("reviews/{id:guid}/manager-review")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SubmitManagerReview(Guid id, [FromBody] SubmitManagerReviewCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Cancel a review that hasn't completed yet. Only the assigned reviewer (or Admin/HR).</summary>
        [HttpPost("reviews/{id:guid}/cancel")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelReview(Guid id, [FromBody] CancelReviewCommand? body, CancellationToken ct)
        {
            await _mediator.Send(new CancelReviewCommand { Id = id, Reason = body?.Reason, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr() }, ct);
            return NoContent();
        }

        /// <summary>Soft-delete a review. Only the assigned reviewer (or Admin/HR).</summary>
        [HttpDelete("reviews/{id:guid}")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteReview(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteReviewCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr() }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted review.</summary>
        [HttpPost("reviews/{id:guid}/restore")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RestoreReview(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreReviewCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr() }, ct);
            return NoContent();
        }

        // ─── Promotions ────────────────────────────────────────────────────────────

        /// <summary>List promotions — paginated, filterable by employeeId/status. Employees see their own; Managers see their own and their reports'; Admin/HR see all.</summary>
        [HttpGet("promotions")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PromotionDto>>), 200)]
        public async Task<IActionResult> GetPromotions([FromQuery] GetPromotionsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsAdminOrHr();
            query.IsManager = User.IsInRole("Manager");
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<PromotionDto>>.Success(result));
        }

        /// <summary>Get a single promotion by ID.</summary>
        [HttpGet("promotions/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PromotionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPromotionById(Guid id, CancellationToken ct)
        {
            var promotion = await _mediator.Send(new GetPromotionByIdQuery { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr(), IsManager = User.IsInRole("Manager") }, ct);
            if (promotion == null) return NotFound();
            return Ok(ApiResponse<PromotionDto>.Success(promotion));
        }

        /// <summary>Propose a promotion for an employee. Manager/Admin/HR only; a Manager may only propose for their own direct reports.</summary>
        [HttpPost("promotions")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> ProposePromotion([FromBody] ProposePromotionCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            cmd.IsManager = User.IsInRole("Manager");
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetPromotionById), new { id }, ApiResponse<object>.Success(new { id }, "Promotion proposed successfully."));
        }

        /// <summary>Approve a proposed promotion — applies the designation/department change to the employee immediately. Admin/HR only.</summary>
        [HttpPost("promotions/{id:guid}/approve")]
        [Authorize(Policy = "CanApprovePromotions")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ApprovePromotion(Guid id, [FromBody] ApprovePromotionCommand? body, CancellationToken ct)
        {
            await _mediator.Send(new ApprovePromotionCommand { Id = id, DecisionNotes = body?.DecisionNotes, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Reject a proposed promotion. Admin/HR only.</summary>
        [HttpPost("promotions/{id:guid}/reject")]
        [Authorize(Policy = "CanApprovePromotions")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RejectPromotion(Guid id, [FromBody] RejectPromotionCommand? body, CancellationToken ct)
        {
            await _mediator.Send(new RejectPromotionCommand { Id = id, DecisionNotes = body?.DecisionNotes, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Withdraw a proposed promotion — the proposer pulling it back. The proposer (or Admin/HR).</summary>
        [HttpPost("promotions/{id:guid}/withdraw")]
        [Authorize(Policy = "CanManagePerformance")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> WithdrawPromotion(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new WithdrawPromotionCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr() }, ct);
            return NoContent();
        }

        /// <summary>Soft-delete a promotion record. Admin/HR only.</summary>
        [HttpDelete("promotions/{id:guid}")]
        [Authorize(Policy = "CanApprovePromotions")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePromotion(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeletePromotionCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted promotion record. Admin/HR only.</summary>
        [HttpPost("promotions/{id:guid}/restore")]
        [Authorize(Policy = "CanApprovePromotions")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RestorePromotion(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestorePromotionCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("User identity could not be resolved.");
            return id;
        }

        private bool IsAdminOrHr() => User.IsInRole("Admin") || User.IsInRole("HR");
    }
}

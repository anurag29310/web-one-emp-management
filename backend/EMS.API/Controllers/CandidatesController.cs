using EMS.Application.Common.DTOs;
using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Features.Recruitment.DTOs;
using EMS.Application.Features.Recruitment.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>
    /// Recruitment &amp; Onboarding: Candidate Management, Interview Scheduling, Offer Generation,
    /// Joining Checklist. All actions require the CanManageRecruitment policy (Admin, HR) except
    /// interview feedback submission, which is scoped to the assigned interviewer (any authenticated
    /// employee) with an Admin/HR override — mirroring the Attendance/Task self-scoping pattern.
    /// </summary>
    [ApiController]
    [Route("api/v1/candidates")]
    [Authorize]
    [Produces("application/json")]
    public class CandidatesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CandidatesController(IMediator mediator) => _mediator = mediator;

        // ─── Candidates ────────────────────────────────────────────────────────────

        /// <summary>List candidates with status/designation/search filters and pagination.</summary>
        [HttpGet]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CandidateDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetCandidatesQuery query, CancellationToken ct)
        {
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<CandidateDto>>.Success(result));
        }

        /// <summary>Get a single candidate by ID.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<CandidateDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var candidate = await _mediator.Send(new GetCandidateByIdQuery { Id = id }, ct);
            if (candidate == null) return NotFound();
            return Ok(ApiResponse<CandidateDto>.Success(CandidateDto.FromEntity(candidate)));
        }

        /// <summary>Register a new candidate application.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateCandidateCommand cmd, CancellationToken ct)
        {
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<object>.Success(new { id }, "Candidate created successfully."));
        }

        /// <summary>Update candidate details.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCandidateCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Soft-delete a candidate.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteCandidateCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted candidate.</summary>
        [HttpPost("{id:guid}/restore")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreCandidateCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Reject a candidate (the company's decision). Terminal — cannot be undone.</summary>
        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] CandidateReasonBody? body, CancellationToken ct)
        {
            await _mediator.Send(new RejectCandidateCommand { Id = id, Reason = body?.Reason }, ct);
            return NoContent();
        }

        /// <summary>Withdraw a candidate (the candidate's own decision). Terminal — cannot be undone.</summary>
        [HttpPost("{id:guid}/withdraw")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Withdraw(Guid id, [FromBody] CandidateReasonBody? body, CancellationToken ct)
        {
            await _mediator.Send(new WithdrawCandidateCommand { Id = id, Reason = body?.Reason }, ct);
            return NoContent();
        }

        // ─── Attachments (resume, etc.) ────────────────────────────────────────────

        /// <summary>List a candidate's uploaded attachments (resume, etc.).</summary>
        [HttpGet("{id:guid}/attachments")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CandidateAttachmentDto>>), 200)]
        public async Task<IActionResult> GetAttachments(Guid id, CancellationToken ct)
        {
            var attachments = await _mediator.Send(new GetCandidateAttachmentsQuery { CandidateId = id }, ct);
            return Ok(ApiResponse<IEnumerable<CandidateAttachmentDto>>.Success(attachments));
        }

        /// <summary>Upload an attachment (resume, etc.). Multipart form upload; PDF/JPEG/PNG only, 10 MB max.</summary>
        [HttpPost("{id:guid}/attachments")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "FILE_REQUIRED", Message = "A file is required." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var cmd = new UploadCandidateAttachmentCommand
            {
                CandidateId = id,
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                Content = ms.ToArray(),
                RequestingUserId = GetCurrentUserId()
            };

            var attachmentId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(attachmentId, "Attachment uploaded."));
        }

        /// <summary>Download a candidate attachment.</summary>
        [HttpGet("attachments/{attachmentId:guid}/download")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadAttachment(Guid attachmentId, CancellationToken ct)
        {
            var result = await _mediator.Send(new DownloadCandidateAttachmentQuery { AttachmentId = attachmentId }, ct);
            if (result == null) return NotFound();
            return File(result.Content, result.ContentType, result.FileName);
        }

        // ─── Interviews ────────────────────────────────────────────────────────────

        /// <summary>List a candidate's interviews.</summary>
        [HttpGet("{id:guid}/interviews")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<InterviewDto>>), 200)]
        public async Task<IActionResult> GetInterviews(Guid id, CancellationToken ct)
        {
            var interviews = await _mediator.Send(new GetInterviewsByCandidateQuery { CandidateId = id }, ct);
            return Ok(ApiResponse<IEnumerable<InterviewDto>>.Success(interviews));
        }

        /// <summary>Schedule an interview round for a candidate.</summary>
        [HttpPost("{id:guid}/interviews")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        public async Task<IActionResult> ScheduleInterview(Guid id, [FromBody] ScheduleInterviewCommand cmd, CancellationToken ct)
        {
            cmd.CandidateId = id;
            var interviewId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(interviewId, "Interview scheduled."));
        }

        /// <summary>Reschedule a Scheduled interview.</summary>
        [HttpPost("~/api/v1/interviews/{id:guid}/reschedule")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RescheduleInterview(Guid id, [FromBody] RescheduleInterviewCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Cancel a Scheduled interview.</summary>
        [HttpPost("~/api/v1/interviews/{id:guid}/cancel")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelInterview(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new CancelInterviewCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Mark a Scheduled interview as a no-show.</summary>
        [HttpPost("~/api/v1/interviews/{id:guid}/no-show")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkInterviewNoShow(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new MarkInterviewNoShowCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Submit interview feedback (rating, outcome). Assigned-interviewer-only unless the caller is Admin/HR.</summary>
        [HttpPost("~/api/v1/interviews/{id:guid}/feedback")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SubmitInterviewFeedback(Guid id, [FromBody] SubmitInterviewFeedbackCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdminOrHr();
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        // ─── Offers ────────────────────────────────────────────────────────────────

        /// <summary>List a candidate's offers.</summary>
        [HttpGet("{id:guid}/offers")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OfferDto>>), 200)]
        public async Task<IActionResult> GetOffers(Guid id, CancellationToken ct)
        {
            var offers = await _mediator.Send(new GetOffersByCandidateQuery { CandidateId = id }, ct);
            return Ok(ApiResponse<IEnumerable<OfferDto>>.Success(offers));
        }

        /// <summary>Create a Draft offer for a candidate.</summary>
        [HttpPost("{id:guid}/offers")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        public async Task<IActionResult> CreateOffer(Guid id, [FromBody] CreateOfferCommand cmd, CancellationToken ct)
        {
            cmd.CandidateId = id;
            var offerId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(offerId, "Offer created."));
        }

        /// <summary>Send a Draft offer (generates the offer letter PDF, moves the candidate to Offered).</summary>
        [HttpPost("~/api/v1/offers/{id:guid}/send")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SendOffer(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new SendOfferCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Record the candidate's acceptance of a Sent offer. Seeds the default onboarding checklist.</summary>
        [HttpPost("~/api/v1/offers/{id:guid}/accept")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AcceptOffer(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new AcceptOfferCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Record the candidate's rejection of a Sent offer.</summary>
        [HttpPost("~/api/v1/offers/{id:guid}/reject")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RejectOffer(Guid id, [FromBody] CandidateReasonBody? body, CancellationToken ct)
        {
            await _mediator.Send(new RejectOfferCommand { Id = id, Reason = body?.Reason }, ct);
            return NoContent();
        }

        /// <summary>Withdraw a Draft or Sent offer (the company's decision).</summary>
        [HttpPost("~/api/v1/offers/{id:guid}/withdraw")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> WithdrawOffer(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new WithdrawOfferCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Download the offer letter PDF for a Sent (or later) offer.</summary>
        [HttpGet("~/api/v1/offers/{id:guid}/download")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadOffer(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(new DownloadOfferQuery { Id = id }, ct);
            if (result == null) return NotFound();
            return File(result.Content, result.ContentType, result.FileName);
        }

        // ─── Onboarding checklist ──────────────────────────────────────────────────

        /// <summary>List a candidate's onboarding checklist items.</summary>
        [HttpGet("{id:guid}/checklist")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OnboardingChecklistItemDto>>), 200)]
        public async Task<IActionResult> GetChecklist(Guid id, CancellationToken ct)
        {
            var items = await _mediator.Send(new GetChecklistItemsQuery { CandidateId = id }, ct);
            return Ok(ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Success(items));
        }

        /// <summary>Add a custom checklist item on top of the default set.</summary>
        [HttpPost("{id:guid}/checklist")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        public async Task<IActionResult> AddChecklistItem(Guid id, [FromBody] AddChecklistItemCommand cmd, CancellationToken ct)
        {
            cmd.CandidateId = id;
            var itemId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(itemId, "Checklist item added."));
        }

        /// <summary>Mark a checklist item complete.</summary>
        [HttpPost("~/api/v1/checklist/{itemId:guid}/complete")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CompleteChecklistItem(Guid itemId, [FromBody] CandidateReasonBody? body, CancellationToken ct)
        {
            await _mediator.Send(new CompleteChecklistItemCommand { Id = itemId, Notes = body?.Reason, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Convert an accepted-offer candidate into a real Employee record.</summary>
        [HttpPost("{id:guid}/convert-to-employee")]
        [Authorize(Policy = "CanManageRecruitment")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> ConvertToEmployee(Guid id, [FromBody] ConvertCandidateToEmployeeCommand cmd, CancellationToken ct)
        {
            cmd.CandidateId = id;
            var employeeId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(employeeId, "Candidate converted to employee."));
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

    public class CandidateReasonBody
    {
        public string? Reason { get; set; }
    }
}

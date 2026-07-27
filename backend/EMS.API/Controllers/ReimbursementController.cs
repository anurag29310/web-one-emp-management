using EMS.Application.Common.DTOs;
using EMS.Application.Features.Reimbursements;
using EMS.Application.Features.Reimbursements.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    /// <summary>
    /// Employee Reimbursement Management. Create/edit/submit/delete/attach are strictly
    /// owner-only — unlike Task Management, there is no Admin "act on behalf of" override, since
    /// requirements.md never grants Admin the ability to edit an employee's claim. Review actions
    /// (start-review/approve/reject/request-changes) require the CanManageReimbursements policy
    /// (Admin only) and additionally block self-approval at the handler level even for Admins.
    /// </summary>
    [ApiController]
    [Route("api/v1/reimbursements")]
    [Authorize]
    [Produces("application/json")]
    [EnableRateLimiting("WriteActionPolicy")]
    public class ReimbursementController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReimbursementController(IMediator mediator) => _mediator = mediator;

        /// <summary>List reimbursements. Non-Admin callers only ever see their own, regardless of filters supplied.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ReimbursementDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetReimbursementsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsAdmin();
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<ReimbursementDto>>.Success(result));
        }

        /// <summary>Get a single reimbursement. Returns 404 for a non-Admin, non-owner caller — existence is not disclosed.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ReimbursementDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var reimbursement = await _mediator.Send(new GetReimbursementByIdQuery { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            if (reimbursement == null) return NotFound();
            return Ok(ApiResponse<ReimbursementDto>.Success(ReimbursementDto.FromEntity(reimbursement)));
        }

        /// <summary>Create a Draft reimbursement request for the caller.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ReimbursementDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateReimbursementCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            var created = await _mediator.Send(cmd, ct);
            var dto = ReimbursementDto.FromEntity(created);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id },
                ApiResponse<ReimbursementDto>.Success(dto, "Reimbursement created successfully."));
        }

        /// <summary>Edit a reimbursement. Owner-only, and only while Draft or ChangesRequested.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ReimbursementDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReimbursementCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            cmd.RequestingUserId = GetCurrentUserId();
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<ReimbursementDto>.Success(ReimbursementDto.FromEntity(updated)));
        }

        /// <summary>Delete a Draft reimbursement. Owner-only.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteReimbursementCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Submit a Draft or ChangesRequested reimbursement for approval. Owner-only.</summary>
        [HttpPost("{id:guid}/submit")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new SubmitReimbursementCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Move a Submitted reimbursement into review.</summary>
        [HttpPost("{id:guid}/start-review")]
        [Authorize(Policy = "CanManageReimbursements")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> StartReview(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new StartReviewReimbursementCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Approve an UnderReview reimbursement. Rejected if the caller is the claimant, even if they hold the Admin role.</summary>
        [HttpPost("{id:guid}/approve")]
        [Authorize(Policy = "CanManageReimbursements")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new ApproveReimbursementCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Reject an UnderReview reimbursement with a required remark.</summary>
        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "CanManageReimbursements")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] ReimbursementRemarksBody body, CancellationToken ct)
        {
            await _mediator.Send(new RejectReimbursementCommand { Id = id, Remarks = body.Remarks, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Request changes on an UnderReview reimbursement with a required remark. Moves it back to the employee for editing.</summary>
        [HttpPost("{id:guid}/request-changes")]
        [Authorize(Policy = "CanManageReimbursements")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RequestChanges(Guid id, [FromBody] ReimbursementRemarksBody body, CancellationToken ct)
        {
            await _mediator.Send(new RequestChangesReimbursementCommand { Id = id, Remarks = body.Remarks, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        // ─── Attachments ───────────────────────────────────────────────────────────

        /// <summary>List a reimbursement's supporting documents.</summary>
        [HttpGet("{id:guid}/attachments")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<ReimbursementAttachmentDto>>), 200)]
        public async Task<IActionResult> GetAttachments(Guid id, CancellationToken ct)
        {
            var attachments = await _mediator.Send(new GetReimbursementAttachmentsQuery { ReimbursementId = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return Ok(ApiResponse<System.Collections.Generic.IEnumerable<ReimbursementAttachmentDto>>.Success(attachments));
        }

        /// <summary>Upload a supporting document. Multipart form upload; PDF/JPEG/PNG only, 10 MB max. Owner-only, blocked once Approved/Rejected/Paid.</summary>
        [HttpPost("{id:guid}/attachments")]
        [EnableRateLimiting("AttachmentUploadPolicy")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "FILE_REQUIRED", Message = "A file is required." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var cmd = new UploadReimbursementAttachmentCommand
            {
                ReimbursementId = id,
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                Content = ms.ToArray(),
                RequestingUserId = GetCurrentUserId()
            };

            var attachmentId = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<Guid>.Success(attachmentId, "Attachment uploaded."));
        }

        /// <summary>Download an attachment.</summary>
        [HttpGet("attachments/{attachmentId:guid}/download")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadAttachment(Guid attachmentId, CancellationToken ct)
        {
            var result = await _mediator.Send(new DownloadReimbursementAttachmentQuery { AttachmentId = attachmentId, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            if (result == null) return NotFound();
            return File(result.Content, result.ContentType, result.FileName);
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("User identity could not be resolved.");
            return id;
        }

        private bool IsAdmin() => User.IsInRole("Admin");
    }

    public class ReimbursementRemarksBody
    {
        public string Remarks { get; set; } = null!;
    }
}

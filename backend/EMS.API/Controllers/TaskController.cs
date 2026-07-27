using EMS.Application.Common.DTOs;
using EMS.Application.Features.Tasks;
using EMS.Application.Features.Tasks.DTOs;
using EMS.Domain.Enums;
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
    /// Task Management. Admin-only for create/edit/reassign/cancel (CanManageTasks policy).
    /// Everything else (accept/reject/start/progress/complete/comments/attachments) is open to any
    /// authenticated user but scoped in the handler to the task's assignee, with Admin able to act
    /// on behalf of anyone — mirrors the Attendance check-in/out privileged-override pattern.
    /// </summary>
    [ApiController]
    [Route("api/v1/tasks")]
    [Authorize]
    [Produces("application/json")]
    [EnableRateLimiting("WriteActionPolicy")]
    public class TaskController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TaskController(IMediator mediator) => _mediator = mediator;

        /// <summary>List tasks. Non-Admin callers only ever see tasks assigned to them, regardless of filters supplied.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TaskItemDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] GetTasksQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsAdmin();
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<TaskItemDto>>.Success(result));
        }

        /// <summary>Get a single task. Returns 404 for non-Admin callers who are not the assignee (existence is not disclosed).</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var task = await _mediator.Send(new GetTaskByIdQuery { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            if (task == null) return NotFound();
            return Ok(ApiResponse<TaskItemDto>.Success(TaskItemDto.FromEntity(task)));
        }

        /// <summary>Create (and assign) a new task.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageTasks")]
        [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateTaskCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            var created = await _mediator.Send(cmd, ct);
            var dto = TaskItemDto.FromEntity(created);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id },
                ApiResponse<TaskItemDto>.Success(dto, "Task created successfully."));
        }

        /// <summary>Edit task details. Rejected once the task is Completed or Cancelled.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "CanManageTasks")]
        [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand cmd, CancellationToken ct)
        {
            if (id != cmd.Id)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "ID_MISMATCH", Message = "Route id does not match body id." });

            cmd.RequestingUserId = GetCurrentUserId();
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<TaskItemDto>.Success(TaskItemDto.FromEntity(updated)));
        }

        /// <summary>Reassign a task to a different employee. Resets status to Assigned.</summary>
        [HttpPost("{id:guid}/reassign")]
        [Authorize(Policy = "CanManageTasks")]
        [ProducesResponseType(typeof(ApiResponse<TaskItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reassign(Guid id, [FromBody] ReassignTaskCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            var updated = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<TaskItemDto>.Success(TaskItemDto.FromEntity(updated)));
        }

        /// <summary>Cancel a task. Cannot cancel a task that is already Completed.</summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = "CanManageTasks")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new CancelTaskCommand { Id = id }, ct);
            return NoContent();
        }

        /// <summary>Accept an Assigned task. Assignee-only unless the caller is Admin.</summary>
        [HttpPost("{id:guid}/accept")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new AcceptTaskCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return NoContent();
        }

        /// <summary>Reject an Assigned task. Assignee-only unless the caller is Admin.</summary>
        [HttpPost("{id:guid}/reject")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTaskBody? body, CancellationToken ct)
        {
            await _mediator.Send(new RejectTaskCommand { Id = id, Reason = body?.Reason, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return NoContent();
        }

        /// <summary>Start an Accepted task (moves to InProgress). Assignee-only unless the caller is Admin.</summary>
        [HttpPost("{id:guid}/start")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Start(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new StartTaskCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return NoContent();
        }

        /// <summary>Update progress — moves an InProgress/OnHold task between those two states. Assignee-only unless the caller is Admin.</summary>
        [HttpPost("{id:guid}/progress")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateTaskProgressCommand cmd, CancellationToken ct)
        {
            cmd.Id = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdmin();
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Mark an InProgress/OnHold task Completed. The task becomes read-only afterward. Assignee-only unless the caller is Admin.</summary>
        [HttpPost("{id:guid}/complete")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new CompleteTaskCommand { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return NoContent();
        }

        // ─── Comments ──────────────────────────────────────────────────────────────

        /// <summary>List a task's progress/notes log.</summary>
        [HttpGet("{id:guid}/comments")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<TaskCommentDto>>), 200)]
        public async Task<IActionResult> GetComments(Guid id, CancellationToken ct)
        {
            var comments = await _mediator.Send(new GetTaskCommentsQuery { TaskId = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return Ok(ApiResponse<System.Collections.Generic.IEnumerable<TaskCommentDto>>.Success(comments));
        }

        /// <summary>Add a note to a task's progress log ("Add notes"). Rejected once the task is Completed or Cancelled.</summary>
        [HttpPost("{id:guid}/comments")]
        [ProducesResponseType(typeof(ApiResponse<TaskCommentDto>), 201)]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] AddTaskCommentCommand cmd, CancellationToken ct)
        {
            cmd.TaskId = id;
            cmd.RequestingUserId = GetCurrentUserId();
            cmd.IsPrivileged = IsAdmin();
            var comment = await _mediator.Send(cmd, ct);
            return Ok(ApiResponse<TaskCommentDto>.Success(TaskCommentDto.FromEntity(comment), "Comment added."));
        }

        // ─── Attachments ───────────────────────────────────────────────────────────

        /// <summary>List a task's attachments (photos/documents).</summary>
        [HttpGet("{id:guid}/attachments")]
        [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.IEnumerable<TaskAttachmentDto>>), 200)]
        public async Task<IActionResult> GetAttachments(Guid id, CancellationToken ct)
        {
            var attachments = await _mediator.Send(new GetTaskAttachmentsQuery { TaskId = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
            return Ok(ApiResponse<System.Collections.Generic.IEnumerable<TaskAttachmentDto>>.Success(attachments));
        }

        /// <summary>Upload an attachment ("Upload photos"). Multipart form upload; PDF/JPEG/PNG only, 10 MB max.</summary>
        [HttpPost("{id:guid}/attachments")]
        [EnableRateLimiting("AttachmentUploadPolicy")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
        public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiErrorResponse { Status = 400, Code = "FILE_REQUIRED", Message = "A file is required." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var cmd = new UploadTaskAttachmentCommand
            {
                TaskId = id,
                FileName = file.FileName,
                ContentType = file.ContentType ?? "application/octet-stream",
                Content = ms.ToArray(),
                RequestingUserId = GetCurrentUserId(),
                IsPrivileged = IsAdmin()
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
            var result = await _mediator.Send(new DownloadTaskAttachmentQuery { AttachmentId = attachmentId, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdmin() }, ct);
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

    public class RejectTaskBody
    {
        public string? Reason { get; set; }
    }
}

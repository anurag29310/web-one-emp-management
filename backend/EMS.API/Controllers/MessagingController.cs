using EMS.Application.Common.DTOs;
using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Features.Messaging.DTOs;
using EMS.Application.Features.Messaging.Queries;
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
    /// Internal Messaging: Conversations (1:1 and group) and Messages. Open messaging — any
    /// authenticated employee can start a conversation with any other employee or their manager;
    /// "Employee Messaging"/"Manager Messaging" in requirements.md are just the two participant
    /// roles, not an access restriction (unlike Performance's manager-tier scoping). Every action
    /// other than Delete/Restore is scoped inside the handler to the caller being an active
    /// participant of the conversation, not by role. Delete/Restore are Admin/HR-only moderation
    /// actions gated by the CanManageMessaging policy.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    [Produces("application/json")]
    public class MessagingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MessagingController(IMediator mediator) => _mediator = mediator;

        /// <summary>List the current user's conversations — paginated, filterable by search (matches conversation title or another participant's name), ordered by most recent activity.</summary>
        [HttpGet("conversations")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ConversationDto>>), 200)]
        public async Task<IActionResult> GetConversations([FromQuery] GetConversationsQuery query, CancellationToken ct)
        {
            query.RequestingUserId = GetCurrentUserId();
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<ConversationDto>>.Success(result));
        }

        /// <summary>Number of conversations with at least one unread message, for a nav badge.</summary>
        [HttpGet("conversations/unread-count")]
        [ProducesResponseType(typeof(ApiResponse<int>), 200)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var count = await _mediator.Send(new GetUnreadConversationCountQuery { RequestingUserId = GetCurrentUserId() }, ct);
            return Ok(ApiResponse<int>.Success(count));
        }

        /// <summary>Get a single conversation with its active participants. Only an active participant (or Admin/HR).</summary>
        [HttpGet("conversations/{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ConversationDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetConversationById(Guid id, CancellationToken ct)
        {
            var conversation = await _mediator.Send(new GetConversationByIdQuery { Id = id, RequestingUserId = GetCurrentUserId(), IsPrivileged = IsAdminOrHr() }, ct);
            if (conversation == null) return NotFound();
            return Ok(ApiResponse<ConversationDto>.Success(conversation));
        }

        /// <summary>Start a conversation with one or more other users and send the first message. Reuses an existing untitled 1:1 conversation between the same two users instead of creating a duplicate.</summary>
        [HttpPost("conversations")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationCommand cmd, CancellationToken ct)
        {
            cmd.RequestingUserId = GetCurrentUserId();
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetConversationById), new { id }, ApiResponse<object>.Success(new { id }, "Conversation created successfully."));
        }

        /// <summary>Add one or more users to a conversation, promoting a 1:1 conversation to a group. Only an active participant.</summary>
        [HttpPost("conversations/{id:guid}/participants")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AddParticipants(Guid id, [FromBody] AddParticipantsCommand cmd, CancellationToken ct)
        {
            cmd.ConversationId = id;
            cmd.RequestingUserId = GetCurrentUserId();
            await _mediator.Send(cmd, ct);
            return NoContent();
        }

        /// <summary>Leave a group conversation. Not available on a direct (1:1) conversation.</summary>
        [HttpPost("conversations/{id:guid}/leave")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> LeaveConversation(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new LeaveConversationCommand { ConversationId = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Soft-delete a conversation. Admin/HR moderation action only.</summary>
        [HttpDelete("conversations/{id:guid}")]
        [Authorize(Policy = "CanManageMessaging")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteConversationCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        /// <summary>Restore a soft-deleted conversation. Admin/HR moderation action only.</summary>
        [HttpPost("conversations/{id:guid}/restore")]
        [Authorize(Policy = "CanManageMessaging")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiErrorResponse), 409)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RestoreConversation(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RestoreConversationCommand { Id = id, RequestingUserId = GetCurrentUserId() }, ct);
            return NoContent();
        }

        // ─── Messages ──────────────────────────────────────────────────────────────

        /// <summary>List messages in a conversation — paginated, newest first. Only an active participant (or Admin/HR).</summary>
        [HttpGet("conversations/{id:guid}/messages")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MessageDto>>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessages(Guid id, [FromQuery] GetMessagesQuery query, CancellationToken ct)
        {
            query.ConversationId = id;
            query.RequestingUserId = GetCurrentUserId();
            query.IsPrivileged = IsAdminOrHr();
            var result = await _mediator.Send(query, ct);
            return Ok(ApiResponse<PagedResult<MessageDto>>.Success(result));
        }

        /// <summary>Send a message in an existing conversation. Only an active participant.</summary>
        [HttpPost("conversations/{id:guid}/messages")]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageCommand cmd, CancellationToken ct)
        {
            cmd.ConversationId = id;
            cmd.RequestingUserId = GetCurrentUserId();
            var messageId = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetMessages), new { id }, ApiResponse<object>.Success(new { id = messageId }, "Message sent."));
        }

        /// <summary>Advance the caller's read watermark for a conversation to now. Only an active participant.</summary>
        [HttpPost("conversations/{id:guid}/read")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> MarkConversationRead(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new MarkConversationReadCommand { ConversationId = id, RequestingUserId = GetCurrentUserId() }, ct);
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

using EMS.Application.Features.Notifications.Commands;
using EMS.Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(CreateNotificationCommand cmd)
        {
            var id = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetForUser), new { userId = cmd.UserId }, id);
        }

        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetForUser(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool onlyUnread = false)
        {
            // allow users to fetch their own notifications or admins
            var current = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (current == null) return Forbid();
            if (User.IsInRole("Admin") == false && Guid.Parse(current) != userId) return Forbid();

            var q = new GetNotificationsQuery { UserId = userId, Page = page, PageSize = pageSize, OnlyUnread = onlyUnread };
            var items = await _mediator.Send(q);
            return Ok(items);
        }

        [HttpPost("{id}/mark-read")]
        [Authorize]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await _mediator.Send(new MarkAsReadCommand { NotificationId = id });
            return NoContent();
        }
    }
}

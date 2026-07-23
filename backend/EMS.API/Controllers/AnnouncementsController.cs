using EMS.Application.Features.Announcements.Commands;
using EMS.Application.Features.Announcements.Queries;
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
    [Authorize]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnnouncementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(CreateAnnouncementCommand cmd)
        {
            cmd.CreatedByUserId = CurrentUserId();
            var id = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool onlyUnread = false)
        {
            var q = new GetAnnouncementsQuery
            {
                UserId = CurrentUserId(),
                RoleName = CurrentRoleName(),
                Page = page,
                PageSize = pageSize,
                OnlyUnread = onlyUnread
            };
            var items = await _mediator.Send(q);
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var q = new GetAnnouncementByIdQuery
            {
                AnnouncementId = id,
                UserId = CurrentUserId(),
                RoleName = CurrentRoleName()
            };
            var item = await _mediator.Send(q);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            await _mediator.Send(new MarkAnnouncementReadCommand { AnnouncementId = id, UserId = CurrentUserId() });
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteAnnouncementCommand { AnnouncementId = id });
            return NoContent();
        }

        private Guid CurrentUserId()
        {
            var current = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return current == null ? Guid.Empty : Guid.Parse(current);
        }

        private string CurrentRoleName()
            => User.FindFirstValue(ClaimTypes.Role) ?? "Employee";
    }
}

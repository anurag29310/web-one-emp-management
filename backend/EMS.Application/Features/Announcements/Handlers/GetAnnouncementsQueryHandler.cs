using EMS.Application.DTOs;
using EMS.Application.Features.Announcements.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Announcements.Handlers
{
    public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, IEnumerable<AnnouncementDto>>
    {
        private readonly IAnnouncementRepository _repo;

        public GetAnnouncementsQueryHandler(IAnnouncementRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<AnnouncementDto>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
        {
            var items = await _repo.GetVisibleForUserAsync(request.UserId, request.RoleName, request.Page, request.PageSize, request.OnlyUnread);

            return items.Select(i => new AnnouncementDto
            {
                Id = i.Announcement.Id,
                Title = i.Announcement.Title,
                Message = i.Announcement.Message,
                Priority = i.Announcement.Priority,
                AudienceType = i.Announcement.AudienceType,
                DepartmentId = i.Announcement.DepartmentId,
                TargetRole = i.Announcement.TargetRole,
                CreatedByUserId = i.Announcement.CreatedByUserId,
                CreatedAtUtc = i.Announcement.CreatedAtUtc,
                ExpiresAtUtc = i.Announcement.ExpiresAtUtc,
                IsReadByMe = i.IsRead
            });
        }
    }
}

using EMS.Application.DTOs;
using EMS.Application.Features.Announcements.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Announcements.Handlers
{
    public class GetAnnouncementByIdQueryHandler : IRequestHandler<GetAnnouncementByIdQuery, AnnouncementDto?>
    {
        private readonly IAnnouncementRepository _repo;

        public GetAnnouncementByIdQueryHandler(IAnnouncementRepository repo)
        {
            _repo = repo;
        }

        public async Task<AnnouncementDto?> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _repo.GetVisibleByIdForUserAsync(request.AnnouncementId, request.UserId, request.RoleName);
            if (result == null) return null;

            var (announcement, isRead) = result.Value;
            return new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Message = announcement.Message,
                Priority = announcement.Priority,
                AudienceType = announcement.AudienceType,
                DepartmentId = announcement.DepartmentId,
                TargetRole = announcement.TargetRole,
                CreatedByUserId = announcement.CreatedByUserId,
                CreatedAtUtc = announcement.CreatedAtUtc,
                ExpiresAtUtc = announcement.ExpiresAtUtc,
                IsReadByMe = isRead
            };
        }
    }
}

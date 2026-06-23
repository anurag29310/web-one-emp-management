using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.Handlers
{
    public class GetNotificationsQueryHandler : IRequestHandler<Queries.GetNotificationsQuery, System.Collections.Generic.IEnumerable<NotificationDto>>
    {
        private readonly INotificationRepository _repo;

        public GetNotificationsQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<System.Collections.Generic.IEnumerable<NotificationDto>> Handle(Queries.GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            var items = await _repo.GetForUserAsync(request.UserId, request.Page, request.PageSize, request.OnlyUnread);
            return items.Select(i => new NotificationDto
            {
                Id = i.Id,
                UserId = i.UserId,
                Title = i.Title,
                Message = i.Message,
                Channel = i.Channel,
                IsRead = i.IsRead,
                CreatedAtUtc = i.CreatedAtUtc,
                ExpiresAtUtc = i.ExpiresAtUtc
            });
        }
    }
}

using EMS.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Notifications.Queries
{
    public class GetNotificationsQuery : IRequest<IEnumerable<NotificationDto>>
    {
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool OnlyUnread { get; set; }
    }
}

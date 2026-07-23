using EMS.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Announcements.Queries
{
    public class GetAnnouncementsQuery : IRequest<IEnumerable<AnnouncementDto>>
    {
        public Guid UserId { get; set; }
        public string RoleName { get; set; } = null!;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool OnlyUnread { get; set; }
    }
}

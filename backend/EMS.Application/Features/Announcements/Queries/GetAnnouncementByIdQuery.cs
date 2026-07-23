using EMS.Application.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Announcements.Queries
{
    public class GetAnnouncementByIdQuery : IRequest<AnnouncementDto?>
    {
        public Guid AnnouncementId { get; set; }
        public Guid UserId { get; set; }
        public string RoleName { get; set; } = null!;
    }
}

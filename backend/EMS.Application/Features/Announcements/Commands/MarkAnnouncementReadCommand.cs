using MediatR;
using System;

namespace EMS.Application.Features.Announcements.Commands
{
    public class MarkAnnouncementReadCommand : IRequest
    {
        public Guid AnnouncementId { get; set; }
        public Guid UserId { get; set; }
    }
}

using MediatR;
using System;

namespace EMS.Application.Features.Announcements.Commands
{
    public class DeleteAnnouncementCommand : IRequest
    {
        public Guid AnnouncementId { get; set; }
    }
}

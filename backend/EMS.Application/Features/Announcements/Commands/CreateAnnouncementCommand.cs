using MediatR;
using System;

namespace EMS.Application.Features.Announcements.Commands
{
    public class CreateAnnouncementCommand : IRequest<Guid>
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Priority { get; set; } = "Normal";
        public string AudienceType { get; set; } = "All";
        public Guid? DepartmentId { get; set; }
        public string? TargetRole { get; set; }
        public Guid CreatedByUserId { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }
}

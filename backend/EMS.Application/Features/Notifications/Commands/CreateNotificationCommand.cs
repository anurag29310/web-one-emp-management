using MediatR;
using System;

namespace EMS.Application.Features.Notifications.Commands
{
    public class CreateNotificationCommand : IRequest<Guid>
    {
        public Guid? UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Channel { get; set; } = "InApp";
        public DateTime? ExpiresAtUtc { get; set; }
    }
}

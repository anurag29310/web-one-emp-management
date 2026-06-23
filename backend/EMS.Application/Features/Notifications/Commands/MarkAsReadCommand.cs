using MediatR;
using System;

namespace EMS.Application.Features.Notifications.Commands
{
    public class MarkAsReadCommand : IRequest
    {
        public Guid NotificationId { get; set; }
        public Guid? ReadBy { get; set; }
    }
}

using EMS.Application.Features.Notifications.Commands;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.Handlers
{
    public class MarkAsReadCommandHandler : IRequestHandler<Commands.MarkAsReadCommand>
    {
        private readonly INotificationRepository _repo;

        public MarkAsReadCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(Commands.MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            var n = await _repo.GetByIdAsync(request.NotificationId);
            if (n == null) throw new Exception("Notification not found");
            n.IsRead = true;
            n.ReadAtUtc = DateTime.UtcNow;
            await _repo.UpdateAsync(n);
            await _repo.SaveChangesAsync();
            return Unit.Value;
        }

        Task IRequestHandler<MarkAsReadCommand>.Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}

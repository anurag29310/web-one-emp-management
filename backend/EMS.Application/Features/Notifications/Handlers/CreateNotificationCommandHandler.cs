using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Notifications.Handlers
{
    public class CreateNotificationCommandHandler : IRequestHandler<Commands.CreateNotificationCommand, Guid>
    {
        private readonly INotificationRepository _repo;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<CreateNotificationCommandHandler> _logger;

        public CreateNotificationCommandHandler(INotificationRepository repo, IEmailSender emailSender, ILogger<CreateNotificationCommandHandler> logger)
        {
            _repo = repo;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Guid> Handle(Commands.CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            var n = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Channel = request.Channel,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = request.ExpiresAtUtc
            };

            await _repo.AddAsync(n);
            await _repo.SaveChangesAsync();

            if (n.Channel == "Email" && n.UserId.HasValue)
            {
                try
                {
                    await _emailSender.SendEmailAsync(n.UserId.Value, n.Title, n.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email notification for {NotificationId}", n.Id);
                }
            }

            return n.Id;
        }
    }
}

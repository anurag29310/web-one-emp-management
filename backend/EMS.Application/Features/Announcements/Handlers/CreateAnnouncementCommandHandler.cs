using EMS.Application.Features.Announcements.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Announcements.Handlers
{
    public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, Guid>
    {
        private readonly IAnnouncementRepository _repo;
        private readonly ILogger<CreateAnnouncementCommandHandler> _logger;

        public CreateAnnouncementCommandHandler(IAnnouncementRepository repo, ILogger<CreateAnnouncementCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
        {
            var announcement = new Announcement
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Message = request.Message,
                Priority = request.Priority,
                AudienceType = request.AudienceType,
                DepartmentId = request.DepartmentId,
                TargetRole = request.TargetRole,
                CreatedByUserId = request.CreatedByUserId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = request.ExpiresAtUtc
            };

            await _repo.AddAsync(announcement);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Announcement {AnnouncementId} broadcast to audience {AudienceType}", announcement.Id, announcement.AudienceType);

            return announcement.Id;
        }
    }
}

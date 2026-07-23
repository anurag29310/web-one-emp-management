using EMS.Application.Features.Announcements.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Announcements.Handlers
{
    public class DeleteAnnouncementCommandHandler : IRequestHandler<DeleteAnnouncementCommand>
    {
        private readonly IAnnouncementRepository _repo;
        private readonly ILogger<DeleteAnnouncementCommandHandler> _logger;

        public DeleteAnnouncementCommandHandler(IAnnouncementRepository repo, ILogger<DeleteAnnouncementCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
        {
            var announcement = await _repo.GetByIdAsync(request.AnnouncementId)
                ?? throw new InvalidOperationException($"Announcement {request.AnnouncementId} not found.");

            await _repo.SoftDeleteAsync(announcement);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Retracted announcement {AnnouncementId}", announcement.Id);
        }
    }
}

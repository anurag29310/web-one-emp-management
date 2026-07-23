using EMS.Application.Features.Announcements.Commands;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Announcements.Handlers
{
    public class MarkAnnouncementReadCommandHandler : IRequestHandler<MarkAnnouncementReadCommand>
    {
        private readonly IAnnouncementRepository _repo;

        public MarkAnnouncementReadCommandHandler(IAnnouncementRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(MarkAnnouncementReadCommand request, CancellationToken cancellationToken)
        {
            var announcement = await _repo.GetByIdAsync(request.AnnouncementId)
                ?? throw new InvalidOperationException($"Announcement {request.AnnouncementId} not found.");

            await _repo.MarkReadAsync(announcement.Id, request.UserId);
            await _repo.SaveChangesAsync();
        }
    }
}

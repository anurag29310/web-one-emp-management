using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.OfficeLocations.Handlers
{
    public class DeleteOfficeLocationCommandHandler : IRequestHandler<DeleteOfficeLocationCommand>
    {
        private readonly IOfficeLocationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DeleteOfficeLocationCommandHandler> _logger;

        public DeleteOfficeLocationCommandHandler(IOfficeLocationRepository repo, ICurrentUserService currentUser, ILogger<DeleteOfficeLocationCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(DeleteOfficeLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await _repo.GetByIdAsync(request.Id, _currentUser.CompanyId!.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Office location {request.Id} not found.");

            location.DeletedAtUtc = DateTime.UtcNow;
            location.DeletedBy = _currentUser.UserId;

            await _repo.DeleteAsync(location, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) office location {LocationId}", location.Id);
        }
    }
}

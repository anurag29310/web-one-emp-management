using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.OfficeLocations.Handlers
{
    public class UpdateOfficeLocationCommandHandler : IRequestHandler<UpdateOfficeLocationCommand, OfficeLocation>
    {
        private readonly IOfficeLocationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<UpdateOfficeLocationCommandHandler> _logger;

        public UpdateOfficeLocationCommandHandler(IOfficeLocationRepository repo, ICurrentUserService currentUser, ILogger<UpdateOfficeLocationCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<OfficeLocation> Handle(UpdateOfficeLocationCommand request, CancellationToken cancellationToken)
        {
            var location = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Office location {request.Id} not found.");

            if (await _repo.CodeExistsAsync(request.Code, request.Id, cancellationToken))
                throw new InvalidOperationException("Office location code already exists.");

            location.Name = request.Name;
            location.Code = request.Code;
            location.AddressLine1 = request.AddressLine1;
            location.AddressLine2 = request.AddressLine2;
            location.City = request.City;
            location.State = request.State;
            location.Country = request.Country;
            location.TimeZoneId = request.TimeZoneId;
            location.UpdatedAtUtc = DateTime.UtcNow;
            location.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(location, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated office location {LocationId}", location.Id);
            return location;
        }
    }
}

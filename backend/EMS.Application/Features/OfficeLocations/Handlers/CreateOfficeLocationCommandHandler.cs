using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.OfficeLocations.Handlers
{
    public class CreateOfficeLocationCommandHandler : IRequestHandler<CreateOfficeLocationCommand, OfficeLocation>
    {
        private readonly IOfficeLocationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateOfficeLocationCommandHandler> _logger;

        public CreateOfficeLocationCommandHandler(IOfficeLocationRepository repo, ICurrentUserService currentUser, ILogger<CreateOfficeLocationCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<OfficeLocation> Handle(CreateOfficeLocationCommand request, CancellationToken cancellationToken)
        {
            var companyId = _currentUser.CompanyId!.Value;
            if (await _repo.CodeExistsAsync(request.Code, companyId, ct: cancellationToken))
                throw new InvalidOperationException("Office location code already exists.");

            var location = new OfficeLocation
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = request.Name,
                Code = request.Code,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                Country = request.Country,
                TimeZoneId = request.TimeZoneId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                GeofenceRadiusMeters = request.GeofenceRadiusMeters,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            await _repo.AddAsync(location, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created office location {LocationName} ({LocationId})", location.Name, location.Id);
            return location;
        }
    }
}

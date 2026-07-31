using EMS.Domain.Entities;
using MediatR;

namespace EMS.Application.Features.OfficeLocations
{
    public class CreateOfficeLocationCommand : IRequest<OfficeLocation>
    {
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public string TimeZoneId { get; set; } = null!;

        /// <summary>Geofencing is only enforced once Latitude, Longitude, and GeofenceRadiusMeters are all set.</summary>
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? GeofenceRadiusMeters { get; set; }
    }
}

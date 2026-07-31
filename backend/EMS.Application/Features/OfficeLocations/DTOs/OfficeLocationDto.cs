using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.OfficeLocations.DTOs
{
    public class OfficeLocationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public string TimeZoneId { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? GeofenceRadiusMeters { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static OfficeLocationDto FromEntity(OfficeLocation o) => new()
        {
            Id = o.Id,
            Name = o.Name,
            Code = o.Code,
            AddressLine1 = o.AddressLine1,
            AddressLine2 = o.AddressLine2,
            City = o.City,
            State = o.State,
            Country = o.Country,
            TimeZoneId = o.TimeZoneId,
            Latitude = o.Latitude,
            Longitude = o.Longitude,
            GeofenceRadiusMeters = o.GeofenceRadiusMeters,
            IsDeleted = o.IsDeleted,
            CreatedAtUtc = o.CreatedAtUtc,
            UpdatedAtUtc = o.UpdatedAtUtc
        };
    }
}

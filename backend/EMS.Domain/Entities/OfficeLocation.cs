using System;

namespace EMS.Domain.Entities
{
    public class OfficeLocation
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

        /// <summary>Office coordinates + allowed Punch In radius. Geofencing is only enforced when all
        /// three are set — nullable so existing/new offices aren't geofenced until deliberately configured.</summary>
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? GeofenceRadiusMeters { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public uint RowVersion { get; set; }
    }
}

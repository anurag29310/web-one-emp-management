using System;

namespace EMS.Domain.Entities
{
    public class Client
    {
        public Guid Id { get; set; }
        public string ClientName { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string ContactPerson { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public string? AlternateMobile { get; set; }
        public string Email { get; set; } = null!;
        public string? GstNumber { get; set; }
        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string? State { get; set; }
        public string Country { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Notes { get; set; }

        /// <summary>Inactive clients cannot receive new tasks (enforced once Task Management ships).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Retired from active workflows but retained for history — distinct from soft delete.</summary>
        public bool IsArchived { get; set; }

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

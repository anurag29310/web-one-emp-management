using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    /// <summary>A tenant of the platform. Every core-org entity (Employee, Department, Team,
    /// Designation, OfficeLocation, LeaveType, Shift) and User is scoped to exactly one Company.
    /// Super Admin users are the only Users with a null CompanyId.</summary>
    public class Company
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public CompanyStatus Status { get; set; } = CompanyStatus.Trial;
        public string Timezone { get; set; } = "UTC";
        public string Currency { get; set; } = "USD";
        public string? LogoUrl { get; set; }

        public DateTime RegisteredAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? SuspendedAtUtc { get; set; }
        public string? SuspendedReason { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public string? RejectedReason { get; set; }

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

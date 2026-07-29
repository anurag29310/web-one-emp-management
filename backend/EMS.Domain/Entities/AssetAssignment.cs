using System;

namespace EMS.Domain.Entities
{
    /// <summary>One allocation-to-return cycle of an Asset to an Employee — "Asset Return Tracking".
    /// ReturnedDate is null while the asset is currently out with the employee.</summary>
    public class AssetAssignment
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Asset? Asset { get; set; }
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        /// <summary>The Admin/HR user who allocated it — not FK-enforced, matching Tasks.AssignedByUserId's loose-reference style.</summary>
        public Guid AssignedByUserId { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string? ConditionAtAssignment { get; set; }

        public DateTime? ReturnedDate { get; set; }
        public string? ConditionAtReturn { get; set; }
        public string? Notes { get; set; }

        // No soft delete — deliberately, matching Tasks/Interviews/Offers. There is no "Delete
        // Assignment" action; Return is the only close-out path and it's a field update, not a
        // deletion, so the full allocation history stays queryable forever.
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}

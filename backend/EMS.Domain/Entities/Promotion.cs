using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class Promotion
    {
        public Guid Id { get; set; }
        public string PromotionNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public Guid FromDesignationId { get; set; }
        public Designation? FromDesignation { get; set; }
        public Guid ToDesignationId { get; set; }
        public Designation? ToDesignation { get; set; }

        public Guid? FromDepartmentId { get; set; }
        public Department? FromDepartment { get; set; }
        public Guid? ToDepartmentId { get; set; }
        public Department? ToDepartment { get; set; }

        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; } = null!;
        public PromotionStatus Status { get; set; } = PromotionStatus.Proposed;

        /// <summary>The proposing user — not FK-enforced, matching AssetAssignment.AssignedByUserId's
        /// loose-reference style.</summary>
        public Guid ProposedByUserId { get; set; }
        public Guid? DecidedByUserId { get; set; }
        public DateTime? DecidedAtUtc { get; set; }
        public string? DecisionNotes { get; set; }

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

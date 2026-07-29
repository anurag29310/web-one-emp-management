using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class PerformanceReview
    {
        public Guid Id { get; set; }
        public string ReviewNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        /// <summary>The manager conducting the review — usually EmployeeId's manager, but not enforced
        /// to be, so Admin/HR can assign any reviewer.</summary>
        public Guid ReviewerEmployeeId { get; set; }
        public Employee? ReviewerEmployee { get; set; }

        public DateTime ReviewPeriodStart { get; set; }
        public DateTime ReviewPeriodEnd { get; set; }
        public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

        public string? SelfAssessment { get; set; }
        public string? ManagerAssessment { get; set; }

        /// <summary>1-5, set only when the manager review is submitted (Status becomes Completed).</summary>
        public decimal? OverallRating { get; set; }

        public DateTime? SelfSubmittedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? Notes { get; set; }

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

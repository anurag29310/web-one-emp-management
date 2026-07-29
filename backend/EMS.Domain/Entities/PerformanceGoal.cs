using EMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities
{
    public class PerformanceGoal
    {
        public Guid Id { get; set; }
        public string GoalNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>Free text (e.g. Sales, Engineering, Leadership) — requirements.md gives no fixed
        /// taxonomy, matching Asset.Category's precedent.</summary>
        public string? Category { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime TargetDate { get; set; }

        /// <summary>Optional weight (e.g. this goal counts for 30% of the review period), 0-100.</summary>
        public decimal? Weight { get; set; }
        public GoalStatus Status { get; set; } = GoalStatus.NotStarted;
        public int ProgressPercent { get; set; }

        public ICollection<PerformanceGoalKpi> Kpis { get; set; } = new List<PerformanceGoalKpi>();

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

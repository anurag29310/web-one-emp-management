using System;

namespace EMS.Domain.Entities
{
    /// <summary>A measurable metric attached to a Goal — "KPI Tracking". Tracking progress is a field
    /// update on CurrentValue, not a new record, matching AssetAssignment.Return's precedent. No soft
    /// delete or independent lifecycle — a child of PerformanceGoal, same shape as OnboardingChecklistItem.</summary>
    public class PerformanceGoalKpi
    {
        public Guid Id { get; set; }
        public Guid GoalId { get; set; }
        public PerformanceGoal? Goal { get; set; }
        public string Name { get; set; } = null!;
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}

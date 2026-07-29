using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Performance.DTOs
{
    public class PerformanceGoalKpiDto
    {
        public Guid Id { get; set; }
        public Guid GoalId { get; set; }
        public string Name { get; set; } = null!;
        public decimal TargetValue { get; set; }
        public decimal CurrentValue { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static PerformanceGoalKpiDto FromEntity(PerformanceGoalKpi k) => new()
        {
            Id = k.Id,
            GoalId = k.GoalId,
            Name = k.Name,
            TargetValue = k.TargetValue,
            CurrentValue = k.CurrentValue,
            Unit = k.Unit,
            Notes = k.Notes,
            CreatedAtUtc = k.CreatedAtUtc,
            UpdatedAtUtc = k.UpdatedAtUtc
        };
    }
}

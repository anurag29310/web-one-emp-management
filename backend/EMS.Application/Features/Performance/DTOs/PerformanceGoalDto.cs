using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EMS.Application.Features.Performance.DTOs
{
    public class PerformanceGoalDto
    {
        public Guid Id { get; set; }
        public string GoalNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime TargetDate { get; set; }
        public decimal? Weight { get; set; }
        public string Status { get; set; } = null!;
        public int ProgressPercent { get; set; }
        public List<PerformanceGoalKpiDto> Kpis { get; set; } = new();
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static PerformanceGoalDto FromEntity(PerformanceGoal g) => new()
        {
            Id = g.Id,
            GoalNumber = g.GoalNumber,
            EmployeeId = g.EmployeeId,
            EmployeeName = g.Employee != null ? $"{g.Employee.FirstName} {g.Employee.LastName}".Trim() : null,
            Title = g.Title,
            Description = g.Description,
            Category = g.Category,
            StartDate = g.StartDate,
            TargetDate = g.TargetDate,
            Weight = g.Weight,
            Status = g.Status.ToString(),
            ProgressPercent = g.ProgressPercent,
            Kpis = g.Kpis?.Select(PerformanceGoalKpiDto.FromEntity).ToList() ?? new List<PerformanceGoalKpiDto>(),
            IsDeleted = g.IsDeleted,
            CreatedAtUtc = g.CreatedAtUtc,
            UpdatedAtUtc = g.UpdatedAtUtc
        };
    }
}

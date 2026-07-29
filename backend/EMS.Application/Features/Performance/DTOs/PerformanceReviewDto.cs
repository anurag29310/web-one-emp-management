using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Performance.DTOs
{
    public class PerformanceReviewDto
    {
        public Guid Id { get; set; }
        public string ReviewNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public Guid ReviewerEmployeeId { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime ReviewPeriodStart { get; set; }
        public DateTime ReviewPeriodEnd { get; set; }
        public string Status { get; set; } = null!;
        public string? SelfAssessment { get; set; }
        public string? ManagerAssessment { get; set; }
        public decimal? OverallRating { get; set; }
        public DateTime? SelfSubmittedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static PerformanceReviewDto FromEntity(PerformanceReview r) => new()
        {
            Id = r.Id,
            ReviewNumber = r.ReviewNumber,
            EmployeeId = r.EmployeeId,
            EmployeeName = r.Employee != null ? $"{r.Employee.FirstName} {r.Employee.LastName}".Trim() : null,
            ReviewerEmployeeId = r.ReviewerEmployeeId,
            ReviewerName = r.ReviewerEmployee != null ? $"{r.ReviewerEmployee.FirstName} {r.ReviewerEmployee.LastName}".Trim() : null,
            ReviewPeriodStart = r.ReviewPeriodStart,
            ReviewPeriodEnd = r.ReviewPeriodEnd,
            Status = r.Status.ToString(),
            SelfAssessment = r.SelfAssessment,
            ManagerAssessment = r.ManagerAssessment,
            OverallRating = r.OverallRating,
            SelfSubmittedAtUtc = r.SelfSubmittedAtUtc,
            CompletedAtUtc = r.CompletedAtUtc,
            Notes = r.Notes,
            IsDeleted = r.IsDeleted,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc
        };
    }
}

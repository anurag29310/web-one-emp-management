using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Performance.DTOs
{
    public class PromotionDto
    {
        public Guid Id { get; set; }
        public string PromotionNumber { get; set; } = null!;
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public Guid FromDesignationId { get; set; }
        public string? FromDesignationName { get; set; }
        public Guid ToDesignationId { get; set; }
        public string? ToDesignationName { get; set; }
        public Guid? FromDepartmentId { get; set; }
        public string? FromDepartmentName { get; set; }
        public Guid? ToDepartmentId { get; set; }
        public string? ToDepartmentName { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public Guid ProposedByUserId { get; set; }
        public Guid? DecidedByUserId { get; set; }
        public DateTime? DecidedAtUtc { get; set; }
        public string? DecisionNotes { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static PromotionDto FromEntity(Promotion p) => new()
        {
            Id = p.Id,
            PromotionNumber = p.PromotionNumber,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee != null ? $"{p.Employee.FirstName} {p.Employee.LastName}".Trim() : null,
            FromDesignationId = p.FromDesignationId,
            FromDesignationName = p.FromDesignation?.Name,
            ToDesignationId = p.ToDesignationId,
            ToDesignationName = p.ToDesignation?.Name,
            FromDepartmentId = p.FromDepartmentId,
            FromDepartmentName = p.FromDepartment?.Name,
            ToDepartmentId = p.ToDepartmentId,
            ToDepartmentName = p.ToDepartment?.Name,
            EffectiveDate = p.EffectiveDate,
            Reason = p.Reason,
            Status = p.Status.ToString(),
            ProposedByUserId = p.ProposedByUserId,
            DecidedByUserId = p.DecidedByUserId,
            DecidedAtUtc = p.DecidedAtUtc,
            DecisionNotes = p.DecisionNotes,
            IsDeleted = p.IsDeleted,
            CreatedAtUtc = p.CreatedAtUtc,
            UpdatedAtUtc = p.UpdatedAtUtc
        };
    }
}

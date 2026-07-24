using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Teams.DTOs
{
    public class TeamDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string Name { get; set; } = null!;
        public string Code { get; set; } = null!;
        public Guid? LeadEmployeeId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static TeamDto FromEntity(Team t) => new()
        {
            Id = t.Id,
            DepartmentId = t.DepartmentId,
            DepartmentName = t.Department?.Name,
            Name = t.Name,
            Code = t.Code,
            LeadEmployeeId = t.LeadEmployeeId,
            IsDeleted = t.IsDeleted,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        };
    }
}

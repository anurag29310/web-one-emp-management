using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Roles.DTOs
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static RoleDto FromEntity(Role r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsDeleted = r.IsDeleted,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc
        };
    }
}

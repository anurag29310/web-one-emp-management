using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Users.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool IsActive { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public Guid? EmployeeId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static UserDto FromEntity(User u) => new()
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            IsActive = u.IsActive,
            RoleId = u.RoleId,
            RoleName = u.Role?.Name,
            EmployeeId = u.EmployeeId,
            IsDeleted = u.IsDeleted,
            CreatedAtUtc = u.CreatedAtUtc,
            UpdatedAtUtc = u.UpdatedAtUtc
        };
    }
}

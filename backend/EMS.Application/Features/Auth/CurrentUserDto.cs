using System;

namespace EMS.Application.Features.Auth
{
    public class CurrentUserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Role { get; set; }
        public bool IsActive { get; set; }
    }
}

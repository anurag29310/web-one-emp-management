using System;

namespace EMS.Application.Features.Auth
{
    public class RegisterUserCommand
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Guid? RoleId { get; set; }
    }
}

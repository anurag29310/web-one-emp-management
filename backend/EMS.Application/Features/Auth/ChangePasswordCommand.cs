using System;

namespace EMS.Application.Features.Auth
{
    public class ChangePasswordCommand
    {
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}

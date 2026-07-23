using System;

namespace EMS.Application.Features.Auth
{
    public class DisableMfaCommand
    {
        public Guid UserId { get; set; }
        public string Password { get; set; } = null!;
    }
}

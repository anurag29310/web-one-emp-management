using System;

namespace EMS.Application.Features.Auth
{
    public class EnableMfaCommand
    {
        public Guid UserId { get; set; }
        public string Code { get; set; } = null!;
    }
}

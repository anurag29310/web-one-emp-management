using System;

namespace EMS.Application.Features.Auth
{
    public class VerifyMfaCommand
    {
        public Guid MfaChallengeId { get; set; }
        public string Code { get; set; } = null!;
    }
}

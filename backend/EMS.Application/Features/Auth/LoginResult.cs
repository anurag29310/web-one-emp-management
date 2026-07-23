using System;

namespace EMS.Application.Features.Auth
{
    public class LoginResult
    {
        // Null when RequiresMfa is true — the caller must complete POST /auth/mfa/verify with
        // MfaChallengeId before tokens are issued.
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresInSeconds { get; set; }

        public bool RequiresMfa { get; set; }
        public Guid? MfaChallengeId { get; set; }
    }
}

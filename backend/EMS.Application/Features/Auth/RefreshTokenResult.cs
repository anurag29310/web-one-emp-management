using System;

namespace EMS.Application.Features.Auth
{
    public class RefreshTokenResult
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
    }
}

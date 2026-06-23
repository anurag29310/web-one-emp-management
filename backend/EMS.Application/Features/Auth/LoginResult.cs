namespace EMS.Application.Features.Auth
{
    public class LoginResult
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public int ExpiresInSeconds { get; set; }
    }
}

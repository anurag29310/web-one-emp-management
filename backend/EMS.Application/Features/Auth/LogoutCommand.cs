namespace EMS.Application.Features.Auth
{
    public class LogoutCommand
    {
        public string RefreshToken { get; set; } = null!;
    }
}

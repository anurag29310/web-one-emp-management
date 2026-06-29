namespace EMS.Application.Features.Auth
{
    public class ResetPasswordCommand
    {
        public string Email { get; set; } = null!;
        public string ResetToken { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}

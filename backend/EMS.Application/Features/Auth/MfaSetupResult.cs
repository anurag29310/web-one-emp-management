namespace EMS.Application.Features.Auth
{
    public class MfaSetupResult
    {
        public string ManualEntryKey { get; set; } = null!;
        public string OtpAuthUri { get; set; } = null!;
    }
}

namespace EMS.Application.Interfaces
{
    // RFC 6238 TOTP (the "Google Authenticator" style 6-digit code), operating on a Base32
    // secret. Callers never see or store the secret in plaintext outside of enrollment — see
    // IMfaSecretProtector for how it's persisted.
    public interface ITotpService
    {
        string GenerateSecret();
        string BuildProvisioningUri(string secretBase32, string accountName, string issuer);
        bool ValidateCode(string secretBase32, string code);
    }
}

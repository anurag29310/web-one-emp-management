using EMS.Application.Interfaces;
using OtpNet;
using System;

namespace EMS.Infrastructure.Services
{
    public class TotpService : ITotpService
    {
        public string GenerateSecret()
        {
            var key = KeyGeneration.GenerateRandomKey(20); // 160-bit, the standard TOTP key size
            return Base32Encoding.ToString(key);
        }

        public string BuildProvisioningUri(string secretBase32, string accountName, string issuer)
        {
            var label = Uri.EscapeDataString($"{issuer}:{accountName}");
            var encodedIssuer = Uri.EscapeDataString(issuer);
            return $"otpauth://totp/{label}?secret={secretBase32}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        public bool ValidateCode(string secretBase32, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var totp = new Totp(Base32Encoding.ToBytes(secretBase32));

            // Accept the adjacent 30s step either side, to tolerate clock drift between server
            // and authenticator app and the brief delay before a user submits a code.
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
    }
}

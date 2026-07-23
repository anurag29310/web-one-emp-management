using EMS.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace EMS.Infrastructure.Services
{
    public class DataProtectionMfaSecretProtector : IMfaSecretProtector
    {
        private readonly IDataProtector _protector;

        public DataProtectionMfaSecretProtector(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("EMS.Mfa.Secret.v1");
        }

        public string Protect(string plaintext) => _protector.Protect(plaintext);

        public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
    }
}

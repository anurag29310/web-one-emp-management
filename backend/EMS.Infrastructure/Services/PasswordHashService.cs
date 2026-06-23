using EMS.Application.Interfaces;
using System;
using System.Security.Cryptography;

namespace EMS.Infrastructure.Services
{
    public class PasswordHashService : IPasswordHashService
    {
        public string Hash(string password)
        {
            // simple PBKDF2
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
            var result = new byte[49];
            Buffer.BlockCopy(salt, 0, result, 1, salt.Length);
            Buffer.BlockCopy(hash, 0, result, 17, hash.Length);
            result[0] = 0x01; // version
            return Convert.ToBase64String(result);
        }

        public bool Verify(string hash, string password)
        {
            try
            {
                var bytes = Convert.FromBase64String(hash);
                if (bytes.Length != 49) return false;
                var salt = new byte[16];
                Buffer.BlockCopy(bytes, 1, salt, 0, 16);
                var storedHash = new byte[32];
                Buffer.BlockCopy(bytes, 17, storedHash, 0, 32);
                var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(storedHash, computed);
            }
            catch
            {
                return false;
            }
        }
    }
}

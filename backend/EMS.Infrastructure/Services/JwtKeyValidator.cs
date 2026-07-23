using System;
using System.Text;

namespace EMS.Infrastructure.Services
{
    public static class JwtKeyValidator
    {
        // HMACSHA256 needs at least 128 bits to run at all, but 256 bits (32 bytes) is the
        // minimum for the key to actually resist brute-force — enforce that floor, not just "works".
        public const int MinimumKeyBytes = 32;

        public static void EnsureValid(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < MinimumKeyBytes)
            {
                throw new InvalidOperationException(
                    $"Jwt:Key is missing or shorter than {MinimumKeyBytes} bytes (256 bits). " +
                    "Configure a strong signing key via the Jwt__Key environment variable or a secrets " +
                    "manager — there is no default fallback, since a shared default would let anyone forge tokens.");
            }
        }
    }
}

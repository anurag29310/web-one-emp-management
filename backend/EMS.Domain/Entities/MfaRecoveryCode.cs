using System;

namespace EMS.Domain.Entities
{
    // One-time backup code issued at MFA enrollment so a lost authenticator device doesn't
    // permanently lock the account out. Stored hashed (same treatment as User.PasswordHash),
    // never in plaintext — the plaintext value is shown to the user exactly once, at generation.
    public class MfaRecoveryCode
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public string CodeHash { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UsedAtUtc { get; set; }
    }
}

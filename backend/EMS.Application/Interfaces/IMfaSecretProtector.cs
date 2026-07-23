namespace EMS.Application.Interfaces
{
    // Encrypts the TOTP secret at rest. Unlike PasswordHash (one-way, only ever compared), the
    // TOTP secret must be recoverable to compute a code for verification, so it's protected
    // (reversible) rather than hashed (one-way).
    public interface IMfaSecretProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedText);
    }
}

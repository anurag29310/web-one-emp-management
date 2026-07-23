using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class DisableMfaCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHashService _passwordHasher;

        public DisableMfaCommandHandler(IAuthRepository repo, IPasswordHashService passwordHasher)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
        }

        public async Task Handle(DisableMfaCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(cmd.UserId, ct);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            // Re-verifying the password (not just requiring an authenticated session) means a
            // stolen or left-open access token alone can't be used to turn MFA off.
            if (!_passwordHasher.Verify(user.PasswordHash, cmd.Password))
                throw new UnauthorizedAccessException("Incorrect password.");

            user.IsMfaEnabled = false;
            user.MfaSecretProtected = null;
            user.MfaEnabledAtUtc = null;
            await _repo.UpdateUserAsync(user, ct);

            await _repo.ReplaceMfaRecoveryCodesAsync(user.Id, Array.Empty<MfaRecoveryCode>(), ct);

            await _repo.SaveChangesAsync(ct);
        }
    }
}

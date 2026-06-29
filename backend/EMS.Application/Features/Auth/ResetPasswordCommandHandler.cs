using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class ResetPasswordCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHashService _passwordHasher;

        public ResetPasswordCommandHandler(IAuthRepository repo, IPasswordHashService passwordHasher)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
        }

        public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct = default)
        {
            var userId = await _repo.ValidatePasswordResetTokenAsync(cmd.ResetToken, ct);
            if (userId == null)
                throw new InvalidOperationException("Password reset token is invalid or has expired.");

            var user = await _repo.GetByUsernameOrEmailAsync(cmd.Email, ct);
            if (user == null || user.Id != userId)
                throw new InvalidOperationException("Password reset token is invalid or has expired.");

            user.PasswordHash = _passwordHasher.Hash(cmd.NewPassword);
            await _repo.UpdatePasswordAsync(user, ct);

            // Revoke all sessions so the user must re-login with the new password
            await _repo.RevokeAllRefreshTokensAsync(user.Id, ct);
            await _repo.InvalidatePasswordResetTokenAsync(cmd.ResetToken, ct);
            await _repo.SaveChangesAsync(ct);
        }
    }
}

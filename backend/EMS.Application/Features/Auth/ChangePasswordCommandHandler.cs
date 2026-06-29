using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class ChangePasswordCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHashService _passwordHasher;

        public ChangePasswordCommandHandler(IAuthRepository repo, IPasswordHashService passwordHasher)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
        }

        public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(cmd.UserId, ct);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (!_passwordHasher.Verify(user.PasswordHash, cmd.CurrentPassword))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            user.PasswordHash = _passwordHasher.Hash(cmd.NewPassword);
            await _repo.UpdatePasswordAsync(user, ct);
            await _repo.SaveChangesAsync(ct);
        }
    }
}

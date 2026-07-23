using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class RegenerateMfaRecoveryCodesCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHashService _passwordHasher;

        public RegenerateMfaRecoveryCodesCommandHandler(IAuthRepository repo, IPasswordHashService passwordHasher)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegenerateMfaRecoveryCodesResult> Handle(RegenerateMfaRecoveryCodesCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(cmd.UserId, ct);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (!user.IsMfaEnabled)
                throw new InvalidOperationException("MFA is not enabled for this account.");

            if (!_passwordHasher.Verify(user.PasswordHash, cmd.Password))
                throw new UnauthorizedAccessException("Incorrect password.");

            var plainCodes = MfaRecoveryCodeGenerator.GenerateCodes();
            var entities = plainCodes.Select(code => new MfaRecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = _passwordHasher.Hash(code),
                CreatedAtUtc = DateTime.UtcNow
            });

            await _repo.ReplaceMfaRecoveryCodesAsync(user.Id, entities, ct);
            await _repo.SaveChangesAsync(ct);

            return new RegenerateMfaRecoveryCodesResult { RecoveryCodes = plainCodes };
        }
    }
}

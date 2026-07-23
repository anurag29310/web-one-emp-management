using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class EnableMfaCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly ITotpService _totp;
        private readonly IMfaSecretProtector _protector;
        private readonly IPasswordHashService _passwordHasher;

        public EnableMfaCommandHandler(IAuthRepository repo, ITotpService totp, IMfaSecretProtector protector, IPasswordHashService passwordHasher)
        {
            _repo = repo;
            _totp = totp;
            _protector = protector;
            _passwordHasher = passwordHasher;
        }

        public async Task<EnableMfaResult> Handle(EnableMfaCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(cmd.UserId, ct);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (user.IsMfaEnabled)
                throw new InvalidOperationException("MFA is already enabled for this account.");

            if (string.IsNullOrEmpty(user.MfaSecretProtected))
                throw new InvalidOperationException("Call POST /auth/mfa/setup before enabling MFA.");

            var secret = _protector.Unprotect(user.MfaSecretProtected);
            if (!_totp.ValidateCode(secret, cmd.Code))
                throw new UnauthorizedAccessException("Invalid authentication code.");

            user.IsMfaEnabled = true;
            user.MfaEnabledAtUtc = DateTime.UtcNow;
            await _repo.UpdateUserAsync(user, ct);

            var plainCodes = MfaRecoveryCodeGenerator.GenerateCodes();
            var entities = plainCodes.Select(code => new MfaRecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = _passwordHasher.Hash(code),
                CreatedAtUtc = DateTime.UtcNow
            });
            await _repo.AddMfaRecoveryCodesAsync(entities, ct);

            await _repo.SaveChangesAsync(ct);

            return new EnableMfaResult { RecoveryCodes = plainCodes };
        }
    }
}

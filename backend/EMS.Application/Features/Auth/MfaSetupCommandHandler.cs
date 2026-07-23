using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class MfaSetupCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly ITotpService _totp;
        private readonly IMfaSecretProtector _protector;

        public MfaSetupCommandHandler(IAuthRepository repo, ITotpService totp, IMfaSecretProtector protector)
        {
            _repo = repo;
            _totp = totp;
            _protector = protector;
        }

        public async Task<MfaSetupResult> Handle(MfaSetupCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(cmd.UserId, ct);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (user.IsMfaEnabled)
                throw new InvalidOperationException("MFA is already enabled for this account.");

            // A fresh secret replaces any previously-started-but-never-confirmed enrollment.
            // MFA only actually turns on once EnableMfaCommand verifies a code against it.
            var secret = _totp.GenerateSecret();
            user.MfaSecretProtected = _protector.Protect(secret);

            await _repo.UpdateUserAsync(user, ct);
            await _repo.SaveChangesAsync(ct);

            return new MfaSetupResult
            {
                ManualEntryKey = secret,
                OtpAuthUri = _totp.BuildProvisioningUri(secret, user.Email, "EMS")
            };
        }
    }
}

using EMS.Application.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class VerifyMfaCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly ITotpService _totp;
        private readonly IMfaSecretProtector _protector;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IJwtTokenService _jwtService;
        private readonly IRefreshTokenService _refreshService;

        public VerifyMfaCommandHandler(
            IAuthRepository repo,
            ITotpService totp,
            IMfaSecretProtector protector,
            IPasswordHashService passwordHasher,
            IJwtTokenService jwtService,
            IRefreshTokenService refreshService)
        {
            _repo = repo;
            _totp = totp;
            _protector = protector;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _refreshService = refreshService;
        }

        public async Task<LoginResult> Handle(VerifyMfaCommand cmd, CancellationToken ct = default)
        {
            var challenge = await _repo.GetMfaChallengeAsync(cmd.MfaChallengeId, ct);

            // One generic failure message regardless of *why* — unknown id, expired, already
            // consumed, or a wrong code — mirrors how Login never reveals which half of the
            // credentials was wrong, so this endpoint can't be used to fingerprint challenge state.
            if (challenge == null || challenge.IsConsumed || challenge.ExpiresAtUtc <= DateTime.UtcNow || challenge.User == null)
                throw new UnauthorizedAccessException("Invalid or expired verification code.");

            var user = challenge.User;
            var verified = false;

            if (!string.IsNullOrEmpty(user.MfaSecretProtected))
            {
                var secret = _protector.Unprotect(user.MfaSecretProtected);
                verified = _totp.ValidateCode(secret, cmd.Code);
            }

            if (!verified)
            {
                var recoveryCodes = await _repo.GetUnusedMfaRecoveryCodesAsync(user.Id, ct);
                var matched = recoveryCodes.FirstOrDefault(rc => _passwordHasher.Verify(rc.CodeHash, cmd.Code));
                if (matched != null)
                {
                    await _repo.MarkMfaRecoveryCodeUsedAsync(matched, ct);
                    verified = true;
                }
            }

            if (!verified)
                throw new UnauthorizedAccessException("Invalid or expired verification code.");

            await _repo.ConsumeMfaChallengeAsync(challenge, ct);

            var access = _jwtService.GenerateAccessToken(user);
            var refresh = _refreshService.CreateRefreshToken(user.Id);
            await _repo.AddRefreshTokenAsync(refresh, ct);
            await _repo.SaveChangesAsync(ct);

            return new LoginResult
            {
                AccessToken = access,
                RefreshToken = refresh.Token,
                ExpiresInSeconds = 900
            };
        }
    }
}

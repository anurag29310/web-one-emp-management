using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System.Threading.Tasks;
using System;

namespace EMS.Application.Features.Auth
{
    public class LoginCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IJwtTokenService _jwtService;
        private readonly IRefreshTokenService _refreshService;

        public LoginCommandHandler(IAuthRepository repo, IPasswordHashService passwordHasher, IJwtTokenService jwtService, IRefreshTokenService refreshService)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _refreshService = refreshService;
        }

        // How long a login can sit pending on its second factor before the client has to
        // restart from POST /auth/login. Kept short since a challenge is only useful to whoever
        // just proved they hold the password.
        private static readonly TimeSpan MfaChallengeLifetime = TimeSpan.FromMinutes(5);

        public async Task<LoginResult> Handle(LoginCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByUsernameOrEmailAsync(cmd.UserNameOrEmail, ct);
            if (user == null || !_passwordHasher.Verify(user.PasswordHash, cmd.Password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled.");

            // Company-null (SuperAdmin) tokens never hit this check. Every other user belongs to
            // exactly one company; a non-Active/Trial company blocks login outright — the same
            // condition TenantStatusMiddleware enforces on every subsequent authenticated request,
            // checked here too so login fails fast with a clear message rather than issuing tokens
            // that would just get rejected on first use.
            if (user.Company != null && user.Company.Status is not (CompanyStatus.Active or CompanyStatus.Trial))
                throw new UnauthorizedAccessException($"This company's account is {user.Company.Status}. Contact your administrator.");

            if (user.IsMfaEnabled)
            {
                var challenge = new MfaChallenge
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.Add(MfaChallengeLifetime),
                    IsConsumed = false
                };

                await _repo.AddMfaChallengeAsync(challenge, ct);
                await _repo.SaveChangesAsync(ct);

                return new LoginResult
                {
                    RequiresMfa = true,
                    MfaChallengeId = challenge.Id
                };
            }

            var access = _jwtService.GenerateAccessToken(user);
            var refresh = _refreshService.CreateRefreshToken(user.Id);

            await _repo.AddRefreshTokenAsync(refresh, ct);
            await _repo.SaveChangesAsync(ct);

            return new LoginResult
            {
                AccessToken = access,
                RefreshToken = refresh.Token,
                ExpiresInSeconds = 900 // 15 minutes default
            };
        }
    }
}

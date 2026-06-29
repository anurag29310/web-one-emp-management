using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class RefreshTokenCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IJwtTokenService _jwtService;
        private readonly IRefreshTokenService _refreshService;

        public RefreshTokenCommandHandler(
            IAuthRepository repo,
            IJwtTokenService jwtService,
            IRefreshTokenService refreshService)
        {
            _repo = repo;
            _jwtService = jwtService;
            _refreshService = refreshService;
        }

        public async Task<RefreshTokenResult> Handle(RefreshTokenCommand cmd, CancellationToken ct = default)
        {
            var existing = await _repo.GetRefreshTokenAsync(cmd.RefreshToken, ct);

            if (existing == null)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            // Reuse detection: if the token is already revoked, revoke the entire family
            if (existing.IsRevoked)
            {
                await _repo.RevokeAllRefreshTokensAsync(existing.UserId, ct);
                await _repo.SaveChangesAsync(ct);
                throw new UnauthorizedAccessException("Refresh token reuse detected. All sessions have been revoked.");
            }

            if (existing.ExpiresAtUtc <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired.");

            // Rotate: revoke old, issue new
            await _repo.RevokeRefreshTokenAsync(existing, ct);

            var newRefresh = _refreshService.CreateRefreshToken(existing.UserId);
            await _repo.AddRefreshTokenAsync(newRefresh, ct);
            await _repo.SaveChangesAsync(ct);

            var accessToken = _jwtService.GenerateAccessToken(existing.User!);

            return new RefreshTokenResult
            {
                AccessToken = accessToken,
                RefreshToken = newRefresh.Token,
                ExpiresAtUtc = newRefresh.ExpiresAtUtc
            };
        }
    }
}

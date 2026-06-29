using EMS.Application.Interfaces;
using EMS.Domain.Entities;
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

        public async Task<LoginResult> Handle(LoginCommand cmd, CancellationToken ct = default)
        {
            var user = await _repo.GetByUsernameOrEmailAsync(cmd.UserNameOrEmail, ct);
            if (user == null || !_passwordHasher.Verify(user.PasswordHash, cmd.Password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled.");

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

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class RegisterUserCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IJwtTokenService _jwtService;
        private readonly IRefreshTokenService _refreshService;

        public RegisterUserCommandHandler(IAuthRepository repo, IPasswordHashService passwordHasher, IJwtTokenService jwtService, IRefreshTokenService refreshService)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _refreshService = refreshService;
        }

        public async Task<LoginResult> Handle(RegisterUserCommand cmd, CancellationToken ct = default)
        {
            var existing = await _repo.GetByUsernameOrEmailAsync(cmd.UserName, ct);
            if (existing != null)
                throw new InvalidOperationException("Username or email already exists.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = cmd.UserName,
                Email = cmd.Email,
                PasswordHash = _passwordHasher.Hash(cmd.Password),
                IsActive = true,
                RoleId = cmd.RoleId
            };

            await _repo.AddUserAsync(user, ct);

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

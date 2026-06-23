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

        public async Task<LoginResult> Handle(RegisterUserCommand cmd)
        {
            // check uniqueness
            var existing = await _repo.GetByUsernameOrEmailAsync(cmd.UserName);
            if (existing != null) throw new InvalidOperationException("UserName or Email already exists");

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = cmd.UserName,
                Email = cmd.Email,
                PasswordHash = _passwordHasher.Hash(cmd.Password),
                IsActive = true,
                RoleId = cmd.RoleId
            };

            await _repo.AddUserAsync(user);

            var access = _jwtService.GenerateAccessToken(user);
            var refresh = _refreshService.CreateRefreshToken(user.Id);

            await _repo.AddRefreshTokenAsync(refresh);
            await _repo.SaveChangesAsync();

            return new LoginResult
            {
                AccessToken = access,
                RefreshToken = refresh.Token,
                ExpiresInSeconds = 900
            };
        }
    }
}

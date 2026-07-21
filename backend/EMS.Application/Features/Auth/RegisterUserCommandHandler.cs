using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class RegisterUserCommandHandler
    {
        private readonly IAuthRepository _repo;
        private readonly IUserRepository _users;
        private readonly IRoleRepository _roles;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IJwtTokenService _jwtService;
        private readonly IRefreshTokenService _refreshService;

        public RegisterUserCommandHandler(
            IAuthRepository repo,
            IUserRepository users,
            IRoleRepository roles,
            IPasswordHashService passwordHasher,
            IJwtTokenService jwtService,
            IRefreshTokenService refreshService)
        {
            _repo = repo;
            _users = users;
            _roles = roles;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _refreshService = refreshService;
        }

        public async Task<LoginResult> Handle(RegisterUserCommand cmd, CancellationToken ct = default)
        {
            var existing = await _repo.GetByUsernameOrEmailAsync(cmd.UserName, ct);
            if (existing != null)
                throw new InvalidOperationException("Username or email already exists.");

            // Bootstrap: the very first account in a fresh deployment becomes Admin, since there
            // would otherwise be no way to create one — self-registration never grants a role
            // (see below), and the Users admin API requires an existing Admin to call it. This
            // check only ever matters once; every registration after the first gets no role.
            // A narrow race is possible if two registrations land at the exact same instant on an
            // empty system, but that requires the deployment to already be reachable by two
            // parties before anyone else knows it exists, which is an acceptable bootstrap risk.
            Guid? roleId = null;
            if (!await _users.AnyExistAsync(ct))
            {
                var adminRole = await _roles.GetByNameAsync("Admin", ct);
                roleId = adminRole?.Id;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = cmd.UserName,
                Email = cmd.Email,
                PasswordHash = _passwordHasher.Hash(cmd.Password),
                IsActive = true,
                RoleId = roleId, // null for every registration except the bootstrap admin above
                CreatedAtUtc = DateTime.UtcNow
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

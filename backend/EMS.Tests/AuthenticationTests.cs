using EMS.Application.Features.Auth;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class AuthenticationTests
    {
        [Fact]
        public async Task Login_Returns_Tokens()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName: "TestLoginDb").Options;
            await using var db = new ApplicationDbContext(options);

            var passwordService = new PasswordHashService();
            var pwd = "Password@123";
            var hashed = passwordService.Hash(pwd);

            var role = new EMS.Domain.Entities.Role { Id = System.Guid.NewGuid(), Name = "Employee" };
            var user = new EMS.Domain.Entities.User { Id = System.Guid.NewGuid(), UserName = "jdoe", Email = "jdoe@example.com", PasswordHash = hashed, Role = role, RoleId = role.Id };
            db.Roles.Add(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var repo = new AuthRepository(db);

            var inMemorySettings = new Dictionary<string, string> {
                { "Jwt:Key", "this-test-signing-key-is-at-least-32-bytes-long!" },
                { "Jwt:Issuer", "ems-test" }
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var jwtService = new JwtTokenService(config);
            var refreshService = new RefreshTokenService();

            var handler = new LoginCommandHandler(repo, passwordService, jwtService, refreshService);

            var cmd = new LoginCommand { UserNameOrEmail = "jdoe", Password = pwd };
            var result = await handler.Handle(cmd);

            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        private static RegisterUserCommandHandler CreateRegisterHandler(ApplicationDbContext db, out AuthRepository authRepo)
        {
            authRepo = new AuthRepository(db);
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var passwordService = new PasswordHashService();

            // JwtTokenService now requires a key of at least 32 bytes (JwtKeyValidator) — anything
            // shorter throws at construction.
            var inMemorySettings = new Dictionary<string, string> {
                { "Jwt:Key", "this-test-signing-key-is-at-least-32-bytes-long!" },
                { "Jwt:Issuer", "ems-test" }
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
            var jwtService = new JwtTokenService(config);
            var refreshService = new RefreshTokenService();

            return new RegisterUserCommandHandler(authRepo, userRepo, roleRepo, passwordService, jwtService, refreshService);
        }

        [Fact]
        public async Task Register_FirstUserInSystem_BootstrapsAsAdmin()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName: "TestRegisterBootstrapDb_" + System.Guid.NewGuid()).Options;
            await using var db = new ApplicationDbContext(options);
            // EnsureCreated() materializes the seeded RBAC roles (HasData) — the InMemory provider
            // only applies them once the store is explicitly created, same as Program.cs at startup.
            await db.Database.EnsureCreatedAsync();

            var handler = CreateRegisterHandler(db, out var authRepo);

            // The very first account in a fresh deployment must become Admin — otherwise there is
            // no way to reach the Users admin API at all, since self-registration never grants a
            // role and that API requires an existing Admin to call it.
            var result = await handler.Handle(new RegisterUserCommand { UserName = "firstuser", Email = "first@example.com", Password = "Password@123" });

            var stored = await authRepo.GetByUsernameOrEmailAsync("firstuser");
            Assert.NotNull(stored);
            Assert.NotNull(stored!.RoleId);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
            var roleClaim = jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value;
            Assert.Equal("Admin", roleClaim);
        }

        [Fact]
        public async Task Register_SecondUserInSystem_NeverGrantsARole_RegardlessOfHandlerInput()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName: "TestRegisterSecondDb_" + System.Guid.NewGuid()).Options;
            await using var db = new ApplicationDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var handler = CreateRegisterHandler(db, out var authRepo);

            // Bootstraps the system so the next registration is no longer "the first user".
            await handler.Handle(new RegisterUserCommand { UserName = "firstuser", Email = "first@example.com", Password = "Password@123" });

            // RegisterUserCommand no longer exposes a RoleId at all — self-registration must never
            // be able to grant a role (previously it trusted a client-supplied RoleId, letting anyone
            // register as Admin). This locks in that every registration after the bootstrap admin
            // ends up with no role assigned and the lowest-privilege "Employee" claim in its token.
            var result = await handler.Handle(new RegisterUserCommand { UserName = "seconduser", Email = "second@example.com", Password = "Password@123" });

            var stored = await authRepo.GetByUsernameOrEmailAsync("seconduser");
            Assert.NotNull(stored);
            Assert.Null(stored!.RoleId);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
            var roleClaim = jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value;
            Assert.Equal("Employee", roleClaim);
            Assert.NotEqual("Admin", roleClaim);
        }
    }
}

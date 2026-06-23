using EMS.Application.Features.Auth;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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
                { "Jwt:Key", "test-key-which-is-long-enough" },
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
    }
}

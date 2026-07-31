using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace EMS.Tests
{
    // Shares the WebApplicationFactory collection defined in RateLimitingTests.cs so every
    // full-host test class in the suite runs sequentially relative to the others (see that
    // file's comment for why building multiple hosts in parallel races on static state).
    [Collection("WebApplicationFactory")]
    public class TenantIsolationTests
    {
        private const string SuperAdminEmail = "superadmin@example.com";
        private const string SuperAdminPassword = "SuperAdmin@123";
        private const string TenantAdminPassword = "TenantAdmin@123";

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private static WebApplicationFactory<Program> CreateFactory()
        {
            Environment.SetEnvironmentVariable("Jwt__Key", "this-test-signing-key-is-at-least-32-bytes-long!");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "ems-test");

            var dbName = "TenantIsolationTestDb_" + Guid.NewGuid();

            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["RateLimiting:Login:PermitLimit"] = "1000",
                        ["RateLimiting:Login:WindowSeconds"] = "3600",
                        ["RateLimiting:Register:PermitLimit"] = "1000",
                        ["RateLimiting:Register:WindowSeconds"] = "3600",
                        ["SuperAdmin:BootstrapEmail"] = SuperAdminEmail,
                        ["SuperAdmin:BootstrapPassword"] = SuperAdminPassword,
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
                });
            });
        }

        private static Company SeedTenantCompanyAndAdmin(WebApplicationFactory<Program> factory, string userName, string email, CompanyStatus status = CompanyStatus.Active)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHashService>();
            var adminRole = db.Roles.First(r => r.Name == "Admin");

            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Tenant " + Guid.NewGuid().ToString("N")[..8],
                Status = status,
                Timezone = "UTC",
                Currency = "USD",
                RegisteredAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Companies.Add(company);

            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = email,
                PasswordHash = passwordHasher.Hash(TenantAdminPassword),
                RoleId = adminRole.Id,
                CompanyId = company.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });

            db.SaveChanges();
            return company;
        }

        private static void SetCompanyStatus(WebApplicationFactory<Program> factory, Guid companyId, CompanyStatus status)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var company = db.Companies.First(c => c.Id == companyId);
            company.Status = status;
            db.SaveChanges();
        }

        private static async Task<string> LoginAndGetAccessTokenAsync(HttpClient client, string userNameOrEmail, string password)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { userNameOrEmail, password });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<LoginResultDto>>(JsonOptions);
            return body!.Data.AccessToken!;
        }

        private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string path, string accessToken)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return request;
        }

        private record ApiEnvelope<T>(T Data, string Message);
        private record LoginResultDto(string? AccessToken, string? RefreshToken, int? ExpiresInSeconds, bool RequiresMfa, Guid? MfaChallengeId);

        [Fact]
        public async Task Login_TenantAdmin_JwtIncludesCompanyIdClaim()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var company = SeedTenantCompanyAndAdmin(factory, "tenant_admin_1", "tenant1@example.com");

            var accessToken = await LoginAndGetAccessTokenAsync(client, "tenant_admin_1", TenantAdminPassword);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

            var companyIdClaim = jwt.Claims.SingleOrDefault(c => c.Type == "company_id");
            Assert.NotNull(companyIdClaim);
            Assert.Equal(company.Id.ToString(), companyIdClaim!.Value);
        }

        [Fact]
        public async Task Login_SuperAdmin_JwtHasNoCompanyIdClaim()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            // Triggers Program.cs's startup bootstrap block by building the host.
            using (var scope = factory.Services.CreateScope()) { }

            var accessToken = await LoginAndGetAccessTokenAsync(client, SuperAdminEmail, SuperAdminPassword);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

            Assert.DoesNotContain(jwt.Claims, c => c.Type == "company_id");
        }

        [Fact]
        public async Task SuperAdmin_NeverBlockedByTenantStatusMiddleware()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using (var scope = factory.Services.CreateScope()) { }

            var accessToken = await LoginAndGetAccessTokenAsync(client, SuperAdminEmail, SuperAdminPassword);

            using var response = await client.SendAsync(AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/me", accessToken));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TenantStatusMiddleware_BlocksAlreadyIssuedTokenAfterCompanyIsSuspended()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var company = SeedTenantCompanyAndAdmin(factory, "tenant_admin_2", "tenant2@example.com");

            var accessToken = await LoginAndGetAccessTokenAsync(client, "tenant_admin_2", TenantAdminPassword);

            // Token was minted while Active — confirm it works before suspension.
            using (var before = await client.SendAsync(AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/me", accessToken)))
                Assert.Equal(HttpStatusCode.OK, before.StatusCode);

            SetCompanyStatus(factory, company.Id, CompanyStatus.Suspended);

            using var after = await client.SendAsync(AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/me", accessToken));

            Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
            var body = await after.Content.ReadAsStringAsync();
            Assert.Contains("COMPANY_SUSPENDED", body);
        }

        [Fact]
        public async Task Login_RejectsSuspendedCompanysAdmin_EvenWithCorrectCredentials()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            var company = SeedTenantCompanyAndAdmin(factory, "tenant_admin_3", "tenant3@example.com");
            SetCompanyStatus(factory, company.Id, CompanyStatus.Suspended);

            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { userNameOrEmail = "tenant_admin_3", password = TenantAdminPassword });

            // ExceptionHandlingMiddleware maps every UnauthorizedAccessException (bad credentials,
            // disabled account, suspended company) to 403 Forbidden — the same convention this
            // codebase already uses for LoginCommandHandler's other login-rejection paths.
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task TenantStatusMiddleware_AllowsRequestsWhileCompanyIsActive()
        {
            using var factory = CreateFactory();
            using var client = factory.CreateClient();
            SeedTenantCompanyAndAdmin(factory, "tenant_admin_4", "tenant4@example.com");

            var accessToken = await LoginAndGetAccessTokenAsync(client, "tenant_admin_4", TenantAdminPassword);
            using var response = await client.SendAsync(AuthenticatedRequest(HttpMethod.Get, "/api/v1/auth/me", accessToken));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
    public class RateLimitingTests
    {
        // Each factory gets its own in-memory database name so it doesn't leak state into the shared
        // "EMS" database Program.cs registers by default. The window defaults deliberately long (well
        // beyond any plausible request latency, even under a loaded CI machine running the full suite
        // in parallel) so the fixed window can never elapse — and silently reset the counter — between
        // a test's sequential requests; only the permit count needs to be small to exhaust the budget.
        private static WebApplicationFactory<Program> CreateFactory(int permitLimit, int windowSeconds = 3600)
        {
            // Program.cs reads Jwt:Key eagerly (before builder.Build()) so it can fail fast on a
            // missing signing key. Config pushed through WithWebHostBuilder(...).ConfigureAppConfiguration
            // only lands once Build() runs internally — too late for that eager read — so the key has
            // to arrive via an environment variable instead, which WebApplication.CreateBuilder(args)
            // already includes as a source at construction time. RateLimiting below doesn't have this
            // problem: it's only read lazily, when the options are first resolved after Build().
            Environment.SetEnvironmentVariable("Jwt__Key", "this-test-signing-key-is-at-least-32-bytes-long!");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "ems-test");

            var dbName = "RateLimitTestDb_" + Guid.NewGuid();

            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["RateLimiting:Login:PermitLimit"] = permitLimit.ToString(),
                        ["RateLimiting:Login:WindowSeconds"] = windowSeconds.ToString(),
                        ["RateLimiting:Register:PermitLimit"] = permitLimit.ToString(),
                        ["RateLimiting:Register:WindowSeconds"] = windowSeconds.ToString(),
                        ["RateLimiting:MfaVerify:PermitLimit"] = permitLimit.ToString(),
                        ["RateLimiting:MfaVerify:WindowSeconds"] = windowSeconds.ToString(),
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Since EF Core 8, AddDbContext also layers configuration through
                    // IDbContextOptionsConfiguration<TContext>; removing only DbContextOptions<TContext>
                    // leaves Program.cs's UseNpgsql(...) call registered alongside UseInMemoryDatabase(...)
                    // below, which EF Core rejects as two providers on one context.
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));
                });
            });
        }

        private static HttpRequestMessage LoginRequest() =>
            new(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { userNameOrEmail = "nouser", password = "wrong-password" })
            };

        private static HttpRequestMessage RegisterRequest() =>
            new(HttpMethod.Post, "/api/v1/auth/register")
            {
                Content = JsonContent.Create(new
                {
                    userName = "user_" + Guid.NewGuid().ToString("N"),
                    email = Guid.NewGuid().ToString("N") + "@example.com",
                    password = "Password@123"
                })
            };

        // No challenge with this id will ever exist against a fresh in-memory database, so every
        // call resolves to a 401 — irrelevant for a rate-limit test, which only cares whether the
        // Nth request becomes 429, exactly like the bad-credentials LoginRequest() above.
        private static HttpRequestMessage MfaVerifyRequest() =>
            new(HttpMethod.Post, "/api/v1/auth/mfa/verify")
            {
                Content = JsonContent.Create(new { mfaChallengeId = Guid.NewGuid(), code = "000000" })
            };

        [Fact]
        public async Task Login_WithinPermitLimit_IsNeverRateLimited()
        {
            using var factory = CreateFactory(permitLimit: 3);
            using var client = factory.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                using var response = await client.SendAsync(LoginRequest());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }

        [Fact]
        public async Task Login_ExceedingPermitLimit_Returns429WithRetryAfterHeader()
        {
            using var factory = CreateFactory(permitLimit: 3);
            using var client = factory.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                using var response = await client.SendAsync(LoginRequest());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }

            using var blocked = await client.SendAsync(LoginRequest());

            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            Assert.True(blocked.Headers.Contains("Retry-After"), "429 response should advertise Retry-After.");

            var body = await blocked.Content.ReadAsStringAsync();
            Assert.Contains("RATE_LIMIT_EXCEEDED", body);
        }

        [Fact]
        public async Task Register_ExceedingPermitLimit_Returns429()
        {
            using var factory = CreateFactory(permitLimit: 3);
            using var client = factory.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                using var response = await client.SendAsync(RegisterRequest());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }

            using var blocked = await client.SendAsync(RegisterRequest());
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        }

        [Fact]
        public async Task LoginAndRegister_RateLimitBudgetsAreIndependent()
        {
            using var factory = CreateFactory(permitLimit: 2);
            using var client = factory.CreateClient();

            for (var i = 0; i < 2; i++)
            {
                using var response = await client.SendAsync(LoginRequest());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }

            using var loginBlocked = await client.SendAsync(LoginRequest());
            Assert.Equal(HttpStatusCode.TooManyRequests, loginBlocked.StatusCode);

            // Login's budget is exhausted, but register has never been called on this client, so it
            // must still be allowed — the two endpoints must not share a single IP-wide bucket.
            using var registerStillAllowed = await client.SendAsync(RegisterRequest());
            Assert.NotEqual(HttpStatusCode.TooManyRequests, registerStillAllowed.StatusCode);
        }

        [Fact]
        public async Task MfaVerify_ExceedingPermitLimit_Returns429()
        {
            // A 6-digit TOTP code is only 1,000,000 possibilities, so this endpoint needs its own
            // tighter budget than password-based endpoints — verified independently here.
            using var factory = CreateFactory(permitLimit: 3);
            using var client = factory.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                using var response = await client.SendAsync(MfaVerifyRequest());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }

            using var blocked = await client.SendAsync(MfaVerifyRequest());
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        }

        [Fact]
        public async Task MfaVerify_RateLimitBudget_IsIndependentOfLoginAndRegister()
        {
            using var factory = CreateFactory(permitLimit: 2);
            using var client = factory.CreateClient();

            for (var i = 0; i < 2; i++)
            {
                using var response = await client.SendAsync(MfaVerifyRequest());
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }

            using var mfaBlocked = await client.SendAsync(MfaVerifyRequest());
            Assert.Equal(HttpStatusCode.TooManyRequests, mfaBlocked.StatusCode);

            using var loginStillAllowed = await client.SendAsync(LoginRequest());
            Assert.NotEqual(HttpStatusCode.TooManyRequests, loginStillAllowed.StatusCode);

            using var registerStillAllowed = await client.SendAsync(RegisterRequest());
            Assert.NotEqual(HttpStatusCode.TooManyRequests, registerStillAllowed.StatusCode);
        }
    }
}

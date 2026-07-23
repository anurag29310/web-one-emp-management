using EMS.Application.Features.Auth;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class MfaTests
    {
        private static ApplicationDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_mfa_test_" + Guid.NewGuid())
                .Options);

        private static IMfaSecretProtector CreateProtector() =>
            // Ephemeral (in-memory-only, non-persisted) key ring — perfect for a short-lived test
            // process where keys never need to survive past the test itself.
            new DataProtectionMfaSecretProtector(new EphemeralDataProtectionProvider());

        private static (AuthRepository Repo, PasswordHashService PasswordHasher, JwtTokenService Jwt, RefreshTokenService Refresh, TotpService Totp, IMfaSecretProtector Protector) CreateServices(ApplicationDbContext db)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-test-signing-key-is-at-least-32-bytes-long!",
                ["Jwt:Issuer"] = "ems-test"
            };
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            return (
                new AuthRepository(db),
                new PasswordHashService(),
                new JwtTokenService(config),
                new RefreshTokenService(),
                new TotpService(),
                CreateProtector());
        }

        private static async Task<User> SeedUserAsync(ApplicationDbContext db, string password, PasswordHashService hasher)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "Employee" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user_" + Guid.NewGuid().ToString("N")[..8],
                Email = Guid.NewGuid() + "@test.local",
                PasswordHash = hasher.Hash(password),
                Role = role,
                RoleId = role.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Roles.Add(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        private static string ComputeValidTotpCode(string secretBase32)
        {
            var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secretBase32));
            return totp.ComputeTotp();
        }

        // ─── Setup ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Setup_GeneratesSecretAndProvisioningUri_WithoutEnablingMfa()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var handler = new MfaSetupCommandHandler(repo, totp, protector);
            var result = await handler.Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);

            Assert.False(string.IsNullOrEmpty(result.ManualEntryKey));
            Assert.StartsWith("otpauth://totp/", result.OtpAuthUri);
            Assert.Contains(result.ManualEntryKey, result.OtpAuthUri);

            var stored = await db.Users.FindAsync(user.Id);
            Assert.False(stored!.IsMfaEnabled);
            Assert.NotNull(stored.MfaSecretProtected);
            Assert.NotEqual(result.ManualEntryKey, stored.MfaSecretProtected); // stored encrypted, not plaintext
        }

        [Fact]
        public async Task Setup_WhenAlreadyEnabled_Throws()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);
            user.IsMfaEnabled = true;
            await db.SaveChangesAsync();

            var handler = new MfaSetupCommandHandler(repo, totp, protector);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None));
        }

        // ─── Enable ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Enable_WithCorrectCode_EnablesMfaAndReturnsTenRecoveryCodes()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setupHandler = new MfaSetupCommandHandler(repo, totp, protector);
            var setup = await setupHandler.Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);
            var code = ComputeValidTotpCode(setup.ManualEntryKey);

            var enableHandler = new EnableMfaCommandHandler(repo, totp, protector, hasher);
            var result = await enableHandler.Handle(new EnableMfaCommand { UserId = user.Id, Code = code }, CancellationToken.None);

            Assert.Equal(10, result.RecoveryCodes.Count);
            Assert.Equal(10, result.RecoveryCodes.Distinct().Count());

            var stored = await db.Users.FindAsync(user.Id);
            Assert.True(stored!.IsMfaEnabled);
            Assert.NotNull(stored.MfaEnabledAtUtc);

            var storedCodes = await db.MfaRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync();
            Assert.Equal(10, storedCodes.Count);
            Assert.All(storedCodes, c => Assert.DoesNotContain(c.CodeHash, result.RecoveryCodes)); // hashed, not plaintext
        }

        [Fact]
        public async Task Enable_WithWrongCode_ThrowsAndDoesNotEnableMfa()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setupHandler = new MfaSetupCommandHandler(repo, totp, protector);
            await setupHandler.Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);

            var enableHandler = new EnableMfaCommandHandler(repo, totp, protector, hasher);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                enableHandler.Handle(new EnableMfaCommand { UserId = user.Id, Code = "000000" }, CancellationToken.None));

            var stored = await db.Users.FindAsync(user.Id);
            Assert.False(stored!.IsMfaEnabled);
        }

        [Fact]
        public async Task Enable_WithoutPriorSetup_Throws()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var enableHandler = new EnableMfaCommandHandler(repo, totp, protector, hasher);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                enableHandler.Handle(new EnableMfaCommand { UserId = user.Id, Code = "123456" }, CancellationToken.None));
        }

        // ─── Login integration ──────────────────────────────────────────────────────

        [Fact]
        public async Task Login_WithMfaEnabled_ReturnsChallengeInsteadOfTokens()
        {
            using var db = CreateDb();
            var (repo, hasher, jwt, refresh, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setup = await new MfaSetupCommandHandler(repo, totp, protector).Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);
            await new EnableMfaCommandHandler(repo, totp, protector, hasher).Handle(
                new EnableMfaCommand { UserId = user.Id, Code = ComputeValidTotpCode(setup.ManualEntryKey) }, CancellationToken.None);

            var loginHandler = new LoginCommandHandler(repo, hasher, jwt, refresh);
            var result = await loginHandler.Handle(new LoginCommand { UserNameOrEmail = user.UserName, Password = "Password@123" }, CancellationToken.None);

            Assert.True(result.RequiresMfa);
            Assert.NotNull(result.MfaChallengeId);
            Assert.Null(result.AccessToken);
            Assert.Null(result.RefreshToken);
        }

        [Fact]
        public async Task Login_WithoutMfaEnabled_ReturnsTokensDirectly()
        {
            using var db = CreateDb();
            var (repo, hasher, jwt, refresh, _, _) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var loginHandler = new LoginCommandHandler(repo, hasher, jwt, refresh);
            var result = await loginHandler.Handle(new LoginCommand { UserNameOrEmail = user.UserName, Password = "Password@123" }, CancellationToken.None);

            Assert.False(result.RequiresMfa);
            Assert.Null(result.MfaChallengeId);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
        }

        // ─── Verify ─────────────────────────────────────────────────────────────────

        private async Task<(User User, string Secret, Guid ChallengeId, IAuthRepository Repo, PasswordHashService Hasher, TotpService Totp, IMfaSecretProtector Protector, JwtTokenService Jwt, RefreshTokenService Refresh, ApplicationDbContext Db)> ArrangeMfaEnabledUserWithPendingChallenge(ApplicationDbContext db)
        {
            var (repo, hasher, jwt, refresh, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setup = await new MfaSetupCommandHandler(repo, totp, protector).Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);
            await new EnableMfaCommandHandler(repo, totp, protector, hasher).Handle(
                new EnableMfaCommand { UserId = user.Id, Code = ComputeValidTotpCode(setup.ManualEntryKey) }, CancellationToken.None);

            var loginHandler = new LoginCommandHandler(repo, hasher, jwt, refresh);
            var login = await loginHandler.Handle(new LoginCommand { UserNameOrEmail = user.UserName, Password = "Password@123" }, CancellationToken.None);

            return (user, setup.ManualEntryKey, login.MfaChallengeId!.Value, repo, hasher, totp, protector, jwt, refresh, db);
        }

        [Fact]
        public async Task Verify_WithCorrectTotpCode_IssuesTokensAndConsumesChallenge()
        {
            using var db = CreateDb();
            var ctx = await ArrangeMfaEnabledUserWithPendingChallenge(db);

            var handler = new VerifyMfaCommandHandler(ctx.Repo, ctx.Totp, ctx.Protector, ctx.Hasher, ctx.Jwt, ctx.Refresh);
            var result = await handler.Handle(new VerifyMfaCommand { MfaChallengeId = ctx.ChallengeId, Code = ComputeValidTotpCode(ctx.Secret) }, CancellationToken.None);

            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

            var challenge = await db.MfaChallenges.FindAsync(ctx.ChallengeId);
            Assert.True(challenge!.IsConsumed);
        }

        [Fact]
        public async Task Verify_SameChallengeTwice_SecondCallThrows()
        {
            using var db = CreateDb();
            var ctx = await ArrangeMfaEnabledUserWithPendingChallenge(db);
            var code = ComputeValidTotpCode(ctx.Secret);

            var handler = new VerifyMfaCommandHandler(ctx.Repo, ctx.Totp, ctx.Protector, ctx.Hasher, ctx.Jwt, ctx.Refresh);
            await handler.Handle(new VerifyMfaCommand { MfaChallengeId = ctx.ChallengeId, Code = code }, CancellationToken.None);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new VerifyMfaCommand { MfaChallengeId = ctx.ChallengeId, Code = code }, CancellationToken.None));
        }

        [Fact]
        public async Task Verify_WithWrongCode_Throws()
        {
            using var db = CreateDb();
            var ctx = await ArrangeMfaEnabledUserWithPendingChallenge(db);

            var handler = new VerifyMfaCommandHandler(ctx.Repo, ctx.Totp, ctx.Protector, ctx.Hasher, ctx.Jwt, ctx.Refresh);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new VerifyMfaCommand { MfaChallengeId = ctx.ChallengeId, Code = "000000" }, CancellationToken.None));
        }

        [Fact]
        public async Task Verify_WithUnknownChallengeId_Throws()
        {
            using var db = CreateDb();
            var (repo, hasher, jwt, refresh, totp, protector) = CreateServices(db);

            var handler = new VerifyMfaCommandHandler(repo, totp, protector, hasher, jwt, refresh);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new VerifyMfaCommand { MfaChallengeId = Guid.NewGuid(), Code = "123456" }, CancellationToken.None));
        }

        [Fact]
        public async Task Verify_WithExpiredChallenge_Throws()
        {
            using var db = CreateDb();
            var ctx = await ArrangeMfaEnabledUserWithPendingChallenge(db);

            var challenge = await db.MfaChallenges.FindAsync(ctx.ChallengeId);
            challenge!.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();

            var handler = new VerifyMfaCommandHandler(ctx.Repo, ctx.Totp, ctx.Protector, ctx.Hasher, ctx.Jwt, ctx.Refresh);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new VerifyMfaCommand { MfaChallengeId = ctx.ChallengeId, Code = ComputeValidTotpCode(ctx.Secret) }, CancellationToken.None));
        }

        [Fact]
        public async Task Verify_WithValidRecoveryCode_IssuesTokensAndConsumesCodeOnce()
        {
            using var db = CreateDb();
            var (repo, hasher, jwt, refresh, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setup = await new MfaSetupCommandHandler(repo, totp, protector).Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);
            var enableResult = await new EnableMfaCommandHandler(repo, totp, protector, hasher).Handle(
                new EnableMfaCommand { UserId = user.Id, Code = ComputeValidTotpCode(setup.ManualEntryKey) }, CancellationToken.None);
            var recoveryCode = enableResult.RecoveryCodes[0];

            var loginHandler = new LoginCommandHandler(repo, hasher, jwt, refresh);
            var login = await loginHandler.Handle(new LoginCommand { UserNameOrEmail = user.UserName, Password = "Password@123" }, CancellationToken.None);

            var verifyHandler = new VerifyMfaCommandHandler(repo, totp, protector, hasher, jwt, refresh);
            var result = await verifyHandler.Handle(new VerifyMfaCommand { MfaChallengeId = login.MfaChallengeId!.Value, Code = recoveryCode }, CancellationToken.None);

            Assert.False(string.IsNullOrEmpty(result.AccessToken));

            var unusedCodes = await repo.GetUnusedMfaRecoveryCodesAsync(user.Id, CancellationToken.None);
            Assert.Equal(9, unusedCodes.Count);

            // The same recovery code cannot be reused against a fresh challenge.
            var secondLogin = await loginHandler.Handle(new LoginCommand { UserNameOrEmail = user.UserName, Password = "Password@123" }, CancellationToken.None);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                verifyHandler.Handle(new VerifyMfaCommand { MfaChallengeId = secondLogin.MfaChallengeId!.Value, Code = recoveryCode }, CancellationToken.None));
        }

        // ─── Disable ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Disable_WithCorrectPassword_DisablesMfaAndClearsRecoveryCodes()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setup = await new MfaSetupCommandHandler(repo, totp, protector).Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);
            await new EnableMfaCommandHandler(repo, totp, protector, hasher).Handle(
                new EnableMfaCommand { UserId = user.Id, Code = ComputeValidTotpCode(setup.ManualEntryKey) }, CancellationToken.None);

            var disableHandler = new DisableMfaCommandHandler(repo, hasher);
            await disableHandler.Handle(new DisableMfaCommand { UserId = user.Id, Password = "Password@123" }, CancellationToken.None);

            var stored = await db.Users.FindAsync(user.Id);
            Assert.False(stored!.IsMfaEnabled);
            Assert.Null(stored.MfaSecretProtected);

            var remainingCodes = await db.MfaRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync();
            Assert.Empty(remainingCodes);
        }

        [Fact]
        public async Task Disable_WithWrongPassword_Throws()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, _, _) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);
            user.IsMfaEnabled = true;
            await db.SaveChangesAsync();

            var disableHandler = new DisableMfaCommandHandler(repo, hasher);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                disableHandler.Handle(new DisableMfaCommand { UserId = user.Id, Password = "WrongPassword@1" }, CancellationToken.None));

            var stored = await db.Users.FindAsync(user.Id);
            Assert.True(stored!.IsMfaEnabled);
        }

        // ─── Regenerate recovery codes ─────────────────────────────────────────────

        [Fact]
        public async Task RegenerateRecoveryCodes_InvalidatesOldCodesAndIssuesTenNewOnes()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, totp, protector) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var setup = await new MfaSetupCommandHandler(repo, totp, protector).Handle(new MfaSetupCommand { UserId = user.Id }, CancellationToken.None);
            var enableResult = await new EnableMfaCommandHandler(repo, totp, protector, hasher).Handle(
                new EnableMfaCommand { UserId = user.Id, Code = ComputeValidTotpCode(setup.ManualEntryKey) }, CancellationToken.None);
            var oldCode = enableResult.RecoveryCodes[0];

            var regenerateHandler = new RegenerateMfaRecoveryCodesCommandHandler(repo, hasher);
            var result = await regenerateHandler.Handle(new RegenerateMfaRecoveryCodesCommand { UserId = user.Id, Password = "Password@123" }, CancellationToken.None);

            Assert.Equal(10, result.RecoveryCodes.Count);
            Assert.DoesNotContain(oldCode, result.RecoveryCodes);

            var storedCodes = await db.MfaRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync();
            Assert.Equal(10, storedCodes.Count);
            Assert.DoesNotContain(storedCodes, c => hasher.Verify(c.CodeHash, oldCode));
        }

        [Fact]
        public async Task RegenerateRecoveryCodes_WhenMfaNotEnabled_Throws()
        {
            using var db = CreateDb();
            var (repo, hasher, _, _, _, _) = CreateServices(db);
            var user = await SeedUserAsync(db, "Password@123", hasher);

            var regenerateHandler = new RegenerateMfaRecoveryCodesCommandHandler(repo, hasher);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                regenerateHandler.Handle(new RegenerateMfaRecoveryCodesCommand { UserId = user.Id, Password = "Password@123" }, CancellationToken.None));
        }
    }
}

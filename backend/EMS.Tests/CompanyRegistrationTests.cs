using EMS.Application.Features.Companies.Commands;
using EMS.Application.Features.Companies.Handlers;
using EMS.Application.Features.Companies.Queries;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class CompanyRegistrationTests
    {
        private static ApplicationDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("ems_company_reg_test_" + Guid.NewGuid()).Options);

        private static IJwtTokenService CreateJwtService()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "this-test-signing-key-is-at-least-32-bytes-long!",
                ["Jwt:Issuer"] = "ems-test"
            }).Build();
            return new JwtTokenService(config);
        }

        private static RegisterCompanyCommandHandler CreateHandler(ApplicationDbContext db, out ICompanyRepository companyRepo, out IPlatformSettingsRepository settingsRepo)
        {
            companyRepo = new CompanyRepository(db);
            settingsRepo = new PlatformSettingsRepository(db);
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var authRepo = new AuthRepository(db);

            return new RegisterCompanyCommandHandler(
                companyRepo, userRepo, roleRepo, settingsRepo,
                new PasswordHashService(), CreateJwtService(), new RefreshTokenService(), authRepo,
                Mock.Of<IAuditLogger>(), NullLogger<RegisterCompanyCommandHandler>.Instance);
        }

        private static RegisterCompanyCommand ValidCommand() => new()
        {
            CompanyName = "New Co " + Guid.NewGuid().ToString("N")[..8],
            Timezone = "UTC",
            Currency = "USD",
            AdminUserName = "admin_" + Guid.NewGuid().ToString("N")[..8],
            AdminEmail = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            AdminPassword = "Password@123"
        };

        [Fact]
        public async Task Handle_WhenApprovalRequired_LandsInPendingApprovalWithNoTokens()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var handler = CreateHandler(db, out var companyRepo, out _);

            var result = await handler.Handle(ValidCommand(), CancellationToken.None);

            Assert.True(result.RequiresApproval);
            Assert.Equal(nameof(CompanyStatus.PendingApproval), result.CompanyStatus);
            Assert.Null(result.AccessToken);

            var company = await companyRepo.GetByIdAsync(result.CompanyId, CancellationToken.None);
            Assert.Equal(CompanyStatus.PendingApproval, company!.Status);
        }

        [Fact]
        public async Task Handle_WhenApprovalNotRequired_LandsInTrialAndIssuesTokens()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var settings = await db.PlatformSettings.FirstAsync(x => x.Id == PlatformSettings.SingletonId);
            settings.RequireApprovalForNewCompanies = false;
            await db.SaveChangesAsync();

            var handler = CreateHandler(db, out var companyRepo, out _);
            var result = await handler.Handle(ValidCommand(), CancellationToken.None);

            Assert.False(result.RequiresApproval);
            Assert.Equal(nameof(CompanyStatus.Trial), result.CompanyStatus);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));

            var company = await companyRepo.GetByIdAsync(result.CompanyId, CancellationToken.None);
            Assert.Equal(CompanyStatus.Trial, company!.Status);
        }

        [Fact]
        public async Task Handle_WhenPublicRegistrationDisabled_Throws()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var settings = await db.PlatformSettings.FirstAsync(x => x.Id == PlatformSettings.SingletonId);
            settings.IsPublicRegistrationEnabled = false;
            await db.SaveChangesAsync();

            var handler = CreateHandler(db, out _, out _);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(ValidCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenAdminEmailAlreadyExists_Throws()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var handler = CreateHandler(db, out _, out _);

            var cmd = ValidCommand();
            await handler.Handle(cmd, CancellationToken.None);

            var duplicate = ValidCommand();
            duplicate.AdminEmail = cmd.AdminEmail;

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(duplicate, CancellationToken.None));
        }

        [Fact]
        public async Task GetPublicRegistrationStatusQueryHandler_ReflectsPlatformSettingsToggle()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var repo = new PlatformSettingsRepository(db);
            var handler = new GetPublicRegistrationStatusQueryHandler(repo);

            Assert.True(await handler.Handle(new GetPublicRegistrationStatusQuery(), CancellationToken.None));

            var settings = await db.PlatformSettings.FirstAsync(x => x.Id == PlatformSettings.SingletonId);
            settings.IsPublicRegistrationEnabled = false;
            await db.SaveChangesAsync();

            Assert.False(await handler.Handle(new GetPublicRegistrationStatusQuery(), CancellationToken.None));
        }

        [Fact]
        public async Task ApproveThenReject_OnlyValidFromPendingApproval()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var handler = CreateHandler(db, out var companyRepo, out _);
            var result = await handler.Handle(ValidCommand(), CancellationToken.None);

            var approveHandler = new ApproveCompanyCommandHandler(companyRepo, Mock.Of<IAuditLogger>(), NullLogger<ApproveCompanyCommandHandler>.Instance);
            await approveHandler.Handle(new ApproveCompanyCommand { Id = result.CompanyId }, CancellationToken.None);

            var approved = await companyRepo.GetByIdAsync(result.CompanyId, CancellationToken.None);
            Assert.Equal(CompanyStatus.Trial, approved!.Status);

            // Already approved (Trial, not PendingApproval) — rejecting now must fail.
            var rejectHandler = new RejectCompanyCommandHandler(companyRepo, Mock.Of<IAuditLogger>(), NullLogger<RejectCompanyCommandHandler>.Instance);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                rejectHandler.Handle(new RejectCompanyCommand { Id = result.CompanyId }, CancellationToken.None));
        }
    }
}

using EMS.Application.Features.Companies.Commands;
using EMS.Application.Features.Companies.Handlers;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class ForceLogoutTests
    {
        private static ApplicationDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("ems_force_logout_test_" + Guid.NewGuid()).Options);

        private static async Task<(Company company, User user1, User user2, RefreshToken token1, RefreshToken token2)> SeedCompanyWithTwoUsers(ApplicationDbContext db)
        {
            var company = new Company { Id = Guid.NewGuid(), Name = "Force Logout Co", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await db.Companies.AddAsync(company);

            var user1 = new User { Id = Guid.NewGuid(), UserName = "u1", Email = "u1@example.com", PasswordHash = "hash", CompanyId = company.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            var user2 = new User { Id = Guid.NewGuid(), UserName = "u2", Email = "u2@example.com", PasswordHash = "hash", CompanyId = company.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            await db.Users.AddRangeAsync(user1, user2);

            var token1 = new RefreshToken { Id = Guid.NewGuid(), UserId = user1.Id, Token = "t1", ExpiresAtUtc = DateTime.UtcNow.AddDays(7), IsRevoked = false, CreatedAtUtc = DateTime.UtcNow };
            var token2 = new RefreshToken { Id = Guid.NewGuid(), UserId = user2.Id, Token = "t2", ExpiresAtUtc = DateTime.UtcNow.AddDays(7), IsRevoked = false, CreatedAtUtc = DateTime.UtcNow };
            await db.RefreshTokens.AddRangeAsync(token1, token2);

            await db.SaveChangesAsync();
            return (company, user1, user2, token1, token2);
        }

        [Fact]
        public async Task ForceLogoutCompanyCommandHandler_RevokesEveryUsersRefreshTokens_WithoutChangingCompanyStatus()
        {
            using var db = CreateDb();
            var (company, _, _, token1, token2) = await SeedCompanyWithTwoUsers(db);

            var companyRepo = new CompanyRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new ForceLogoutCompanyCommandHandler(companyRepo, authRepo, Mock.Of<IAuditLogger>(), NullLogger<ForceLogoutCompanyCommandHandler>.Instance);

            await handler.Handle(new ForceLogoutCompanyCommand { Id = company.Id }, CancellationToken.None);

            Assert.True((await db.RefreshTokens.FirstAsync(t => t.Id == token1.Id)).IsRevoked);
            Assert.True((await db.RefreshTokens.FirstAsync(t => t.Id == token2.Id)).IsRevoked);

            var reloadedCompany = await companyRepo.GetByIdAsync(company.Id, CancellationToken.None);
            Assert.Equal(CompanyStatus.Active, reloadedCompany!.Status);
        }

        [Fact]
        public async Task ForceLogoutCompanyCommandHandler_DoesNotAffectOtherCompaniesTokens()
        {
            using var db = CreateDb();
            var (company, _, _, token1, token2) = await SeedCompanyWithTwoUsers(db);

            var otherCompany = new Company { Id = Guid.NewGuid(), Name = "Other Co", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            var otherUser = new User { Id = Guid.NewGuid(), UserName = "other", Email = "other@example.com", PasswordHash = "hash", CompanyId = otherCompany.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            var otherToken = new RefreshToken { Id = Guid.NewGuid(), UserId = otherUser.Id, Token = "t-other", ExpiresAtUtc = DateTime.UtcNow.AddDays(7), IsRevoked = false, CreatedAtUtc = DateTime.UtcNow };
            await db.Companies.AddAsync(otherCompany);
            await db.Users.AddAsync(otherUser);
            await db.RefreshTokens.AddAsync(otherToken);
            await db.SaveChangesAsync();

            var companyRepo = new CompanyRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new ForceLogoutCompanyCommandHandler(companyRepo, authRepo, Mock.Of<IAuditLogger>(), NullLogger<ForceLogoutCompanyCommandHandler>.Instance);

            await handler.Handle(new ForceLogoutCompanyCommand { Id = company.Id }, CancellationToken.None);

            Assert.True((await db.RefreshTokens.FirstAsync(t => t.Id == token1.Id)).IsRevoked);
            Assert.True((await db.RefreshTokens.FirstAsync(t => t.Id == token2.Id)).IsRevoked);
            Assert.False((await db.RefreshTokens.FirstAsync(t => t.Id == otherToken.Id)).IsRevoked);
        }

        [Fact]
        public async Task SuspendCompanyCommandHandler_AlsoRevokesEveryUsersRefreshTokensAsASideEffect()
        {
            using var db = CreateDb();
            var (company, _, _, token1, token2) = await SeedCompanyWithTwoUsers(db);

            var companyRepo = new CompanyRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new SuspendCompanyCommandHandler(companyRepo, authRepo, Mock.Of<IAuditLogger>(), NullLogger<SuspendCompanyCommandHandler>.Instance);

            await handler.Handle(new SuspendCompanyCommand { Id = company.Id, Reason = "fraud" }, CancellationToken.None);

            Assert.True((await db.RefreshTokens.FirstAsync(t => t.Id == token1.Id)).IsRevoked);
            Assert.True((await db.RefreshTokens.FirstAsync(t => t.Id == token2.Id)).IsRevoked);

            var reloadedCompany = await companyRepo.GetByIdAsync(company.Id, CancellationToken.None);
            Assert.Equal(CompanyStatus.Suspended, reloadedCompany!.Status);
        }

        [Fact]
        public async Task ForceLogoutCompanyCommandHandler_ThrowsWhenCompanyNotFound()
        {
            using var db = CreateDb();
            var companyRepo = new CompanyRepository(db);
            var authRepo = new AuthRepository(db);
            var handler = new ForceLogoutCompanyCommandHandler(companyRepo, authRepo, Mock.Of<IAuditLogger>(), NullLogger<ForceLogoutCompanyCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new ForceLogoutCompanyCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }
    }
}

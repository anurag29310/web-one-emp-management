using EMS.Application.Features.Companies.Commands;
using EMS.Application.Features.Companies.Handlers;
using EMS.Application.Features.Companies.Queries;
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
    public class CompaniesTests
    {
        private static ApplicationDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("ems_companies_test_" + Guid.NewGuid()).Options);

        private static IAuditLogger NoopAuditLogger() => Mock.Of<IAuditLogger>();

        [Fact]
        public async Task CreateCompanyCommandHandler_CreatesActiveCompany()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            var handler = new CreateCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<CreateCompanyCommandHandler>.Instance);

            var result = await handler.Handle(new CreateCompanyCommand { Name = "Acme Inc", Timezone = "UTC", Currency = "USD" }, CancellationToken.None);

            Assert.Equal(CompanyStatus.Active, result.Status);
            Assert.NotNull(await repo.GetByIdAsync(result.Id, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateCompanyCommandHandler_UpdatesFields()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            var createHandler = new CreateCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<CreateCompanyCommandHandler>.Instance);
            var company = await createHandler.Handle(new CreateCompanyCommand { Name = "Old Name", Timezone = "UTC", Currency = "USD" }, CancellationToken.None);

            var updateHandler = new UpdateCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<UpdateCompanyCommandHandler>.Instance);
            var updated = await updateHandler.Handle(new UpdateCompanyCommand { Id = company.Id, Name = "New Name", Timezone = "IST", Currency = "INR" }, CancellationToken.None);

            Assert.Equal("New Name", updated.Name);
            Assert.Equal("IST", updated.Timezone);
            Assert.Equal("INR", updated.Currency);
        }

        [Fact]
        public async Task DeleteThenRestoreCompanyCommandHandler_RoundTrips()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            var createHandler = new CreateCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<CreateCompanyCommandHandler>.Instance);
            var company = await createHandler.Handle(new CreateCompanyCommand { Name = "ToDelete", Timezone = "UTC", Currency = "USD" }, CancellationToken.None);

            var deleteHandler = new DeleteCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<DeleteCompanyCommandHandler>.Instance);
            await deleteHandler.Handle(new DeleteCompanyCommand { Id = company.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(company.Id, CancellationToken.None));
            var deleted = await repo.GetByIdIncludingDeletedAsync(company.Id, CancellationToken.None);
            Assert.True(deleted!.IsDeleted);

            var restoreHandler = new RestoreCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<RestoreCompanyCommandHandler>.Instance);
            await restoreHandler.Handle(new RestoreCompanyCommand { Id = company.Id }, CancellationToken.None);

            Assert.NotNull(await repo.GetByIdAsync(company.Id, CancellationToken.None));
        }

        [Fact]
        public async Task SuspendCompanyCommandHandler_SetsSuspendedStatusAndRevokesRefreshTokens()
        {
            using var db = CreateDb();
            var companyRepo = new CompanyRepository(db);
            var authRepo = new AuthRepository(db);

            var company = new Company { Id = Guid.NewGuid(), Name = "Suspend Me", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await companyRepo.AddAsync(company, CancellationToken.None);

            var user = new User { Id = Guid.NewGuid(), UserName = "admin1", Email = "admin1@example.com", PasswordHash = "hash", CompanyId = company.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            await db.Users.AddAsync(user);
            var token = new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, Token = "tok-1", ExpiresAtUtc = DateTime.UtcNow.AddDays(7), IsRevoked = false, CreatedAtUtc = DateTime.UtcNow };
            await db.RefreshTokens.AddAsync(token);
            await db.SaveChangesAsync();

            var handler = new SuspendCompanyCommandHandler(companyRepo, authRepo, NoopAuditLogger(), NullLogger<SuspendCompanyCommandHandler>.Instance);
            await handler.Handle(new SuspendCompanyCommand { Id = company.Id, Reason = "Non-payment" }, CancellationToken.None);

            var reloaded = await companyRepo.GetByIdAsync(company.Id, CancellationToken.None);
            Assert.Equal(CompanyStatus.Suspended, reloaded!.Status);
            Assert.Equal("Non-payment", reloaded.SuspendedReason);
            Assert.NotNull(reloaded.SuspendedAtUtc);

            var reloadedToken = await db.RefreshTokens.FirstAsync(t => t.Id == token.Id);
            Assert.True(reloadedToken.IsRevoked);
        }

        [Fact]
        public async Task ActivateCompanyCommandHandler_ClearsSuspensionFields()
        {
            using var db = CreateDb();
            var companyRepo = new CompanyRepository(db);
            var authRepo = new AuthRepository(db);

            var company = new Company { Id = Guid.NewGuid(), Name = "Reactivate Me", Status = CompanyStatus.Suspended, SuspendedAtUtc = DateTime.UtcNow, SuspendedReason = "test", Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await companyRepo.AddAsync(company, CancellationToken.None);
            await db.SaveChangesAsync();

            var handler = new ActivateCompanyCommandHandler(companyRepo, NoopAuditLogger(), NullLogger<ActivateCompanyCommandHandler>.Instance);
            await handler.Handle(new ActivateCompanyCommand { Id = company.Id }, CancellationToken.None);

            var reloaded = await companyRepo.GetByIdAsync(company.Id, CancellationToken.None);
            Assert.Equal(CompanyStatus.Active, reloaded!.Status);
            Assert.Null(reloaded.SuspendedAtUtc);
            Assert.Null(reloaded.SuspendedReason);
        }

        [Fact]
        public async Task ApproveCompanyCommandHandler_MovesPendingApprovalToTrial()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            var company = new Company { Id = Guid.NewGuid(), Name = "Pending Co", Status = CompanyStatus.PendingApproval, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(company, CancellationToken.None);
            await db.SaveChangesAsync();

            var handler = new ApproveCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<ApproveCompanyCommandHandler>.Instance);
            await handler.Handle(new ApproveCompanyCommand { Id = company.Id }, CancellationToken.None);

            var reloaded = await repo.GetByIdAsync(company.Id, CancellationToken.None);
            Assert.Equal(CompanyStatus.Trial, reloaded!.Status);
            Assert.NotNull(reloaded.ApprovedAtUtc);
        }

        [Fact]
        public async Task ApproveCompanyCommandHandler_ThrowsWhenNotPendingApproval()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            var company = new Company { Id = Guid.NewGuid(), Name = "Already Active", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(company, CancellationToken.None);
            await db.SaveChangesAsync();

            var handler = new ApproveCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<ApproveCompanyCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new ApproveCompanyCommand { Id = company.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task RejectCompanyCommandHandler_MovesPendingApprovalToRejectedWithReason()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            var company = new Company { Id = Guid.NewGuid(), Name = "Pending Co 2", Status = CompanyStatus.PendingApproval, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(company, CancellationToken.None);
            await db.SaveChangesAsync();

            var handler = new RejectCompanyCommandHandler(repo, NoopAuditLogger(), NullLogger<RejectCompanyCommandHandler>.Instance);
            await handler.Handle(new RejectCompanyCommand { Id = company.Id, Reason = "Duplicate registration" }, CancellationToken.None);

            var reloaded = await repo.GetByIdAsync(company.Id, CancellationToken.None);
            Assert.Equal(CompanyStatus.Rejected, reloaded!.Status);
            Assert.Equal("Duplicate registration", reloaded.RejectedReason);
        }

        [Fact]
        public async Task GetCompaniesQueryHandler_FiltersByStatusAndSearch()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);
            await repo.AddAsync(new Company { Id = Guid.NewGuid(), Name = "Acme Corp", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow }, CancellationToken.None);
            await repo.AddAsync(new Company { Id = Guid.NewGuid(), Name = "Beta LLC", Status = CompanyStatus.Suspended, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow }, CancellationToken.None);
            await db.SaveChangesAsync();

            var handler = new GetCompaniesQueryHandler(repo);
            var activeOnly = await handler.Handle(new GetCompaniesQuery { Status = CompanyStatus.Active }, CancellationToken.None);
            Assert.Equal(1, activeOnly.TotalCount);
            Assert.Equal("Acme Corp", activeOnly.Data.Single().Name);

            var searched = await handler.Handle(new GetCompaniesQuery { Search = "Beta" }, CancellationToken.None);
            Assert.Equal(1, searched.TotalCount);
            Assert.Equal("Beta LLC", searched.Data.Single().Name);
        }

        [Fact]
        public async Task GetCompanyByIdQueryHandler_IncludesEmployeeCountAndAdmins()
        {
            using var db = CreateDb();
            await db.Database.EnsureCreatedAsync();
            var repo = new CompanyRepository(db);

            var company = new Company { Id = Guid.NewGuid(), Name = "Detail Co", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(company, CancellationToken.None);

            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");
            var admin = new User { Id = Guid.NewGuid(), UserName = "detailadmin", Email = "detailadmin@example.com", PasswordHash = "hash", CompanyId = company.Id, RoleId = adminRole.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            await db.Users.AddAsync(admin);

            await db.Employees.AddAsync(new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "B", CompanyId = company.Id, JoinDate = DateTime.UtcNow, IsActive = true });
            await db.SaveChangesAsync();

            var handler = new GetCompanyByIdQueryHandler(repo);
            var detail = await handler.Handle(new GetCompanyByIdQuery { Id = company.Id }, CancellationToken.None);

            Assert.NotNull(detail);
            Assert.Equal(1, detail!.EmployeeCount);
            var returnedAdmin = Assert.Single(detail.Admins);
            Assert.Equal("detailadmin", returnedAdmin.UserName);
        }

        // ─── Tenant isolation ──────────────────────────────────────────────────────

        [Fact]
        public async Task DesignationRepository_AllowsSameCodeAtTwoDifferentCompanies()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            var companyA = Guid.NewGuid();
            var companyB = Guid.NewGuid();

            var deptA = new Designation { Id = Guid.NewGuid(), Name = "Engineer", Code = "ENG-1", CompanyId = companyA, CreatedAtUtc = DateTime.UtcNow };
            var deptB = new Designation { Id = Guid.NewGuid(), Name = "Engineer", Code = "ENG-1", CompanyId = companyB, CreatedAtUtc = DateTime.UtcNow };

            await repo.AddAsync(deptA, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);
            await repo.AddAsync(deptB, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            var existsForA = await repo.CodeExistsAsync("ENG-1", companyA, ct: CancellationToken.None);
            var existsForB = await repo.CodeExistsAsync("ENG-1", companyB, ct: CancellationToken.None);
            Assert.True(existsForA);
            Assert.True(existsForB);

            var onlyA = await repo.GetAllAsync(companyA, CancellationToken.None);
            Assert.Single(onlyA);
            Assert.Equal(deptA.Id, onlyA.Single().Id);
        }
    }
}

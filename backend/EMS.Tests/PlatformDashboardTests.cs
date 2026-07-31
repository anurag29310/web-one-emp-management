using EMS.Application.Features.PlatformDashboard.Handlers;
using EMS.Application.Features.PlatformDashboard.Queries;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class PlatformDashboardTests
    {
        private static ApplicationDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("ems_platform_dashboard_test_" + Guid.NewGuid()).Options);

        [Fact]
        public async Task Handle_ReturnsStatusCountsTotalEmployeesAndRecentRegistrations()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);

            var active1 = new Company { Id = Guid.NewGuid(), Name = "Active One", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow.AddDays(-2), CreatedAtUtc = DateTime.UtcNow };
            var active2 = new Company { Id = Guid.NewGuid(), Name = "Active Two", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow.AddDays(-1), CreatedAtUtc = DateTime.UtcNow };
            var suspended = new Company { Id = Guid.NewGuid(), Name = "Suspended One", Status = CompanyStatus.Suspended, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow.AddDays(-3), CreatedAtUtc = DateTime.UtcNow };
            var trial = new Company { Id = Guid.NewGuid(), Name = "Trial One", Status = CompanyStatus.Trial, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };

            await db.Companies.AddRangeAsync(active1, active2, suspended, trial);
            await db.Employees.AddRangeAsync(
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E1", FirstName = "A", LastName = "1", CompanyId = active1.Id, JoinDate = DateTime.UtcNow, IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E2", FirstName = "B", LastName = "2", CompanyId = active2.Id, JoinDate = DateTime.UtcNow, IsActive = true },
                new Employee { Id = Guid.NewGuid(), EmployeeCode = "E3", FirstName = "C", LastName = "3", CompanyId = suspended.Id, JoinDate = DateTime.UtcNow, IsActive = false });
            await db.SaveChangesAsync();

            var handler = new GetPlatformDashboardSummaryQueryHandler(repo, NullLogger<GetPlatformDashboardSummaryQueryHandler>.Instance);
            var result = await handler.Handle(new GetPlatformDashboardSummaryQuery { RecentCount = 2 }, CancellationToken.None);

            Assert.Equal(4, result.TotalCompanies);
            Assert.Equal(2, result.ActiveCompanies);
            Assert.Equal(1, result.SuspendedCompanies);
            Assert.Equal(1, result.TrialCompanies);
            Assert.Equal(3, result.TotalEmployeesAcrossAllCompanies);
            Assert.Equal(2, result.RecentRegistrations.Count);
            // Most recently registered first.
            Assert.Equal(trial.Id, result.RecentRegistrations[0].Id);
        }

        [Fact]
        public async Task Handle_ExcludesSoftDeletedCompaniesFromCounts()
        {
            using var db = CreateDb();
            var repo = new CompanyRepository(db);

            var active = new Company { Id = Guid.NewGuid(), Name = "Still Here", Status = CompanyStatus.Active, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            var deleted = new Company { Id = Guid.NewGuid(), Name = "Gone", Status = CompanyStatus.Active, IsDeleted = true, Timezone = "UTC", Currency = "USD", RegisteredAtUtc = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow };
            await db.Companies.AddRangeAsync(active, deleted);
            await db.SaveChangesAsync();

            var handler = new GetPlatformDashboardSummaryQueryHandler(repo, NullLogger<GetPlatformDashboardSummaryQueryHandler>.Instance);
            var result = await handler.Handle(new GetPlatformDashboardSummaryQuery(), CancellationToken.None);

            Assert.Equal(1, result.TotalCompanies);
        }
    }
}

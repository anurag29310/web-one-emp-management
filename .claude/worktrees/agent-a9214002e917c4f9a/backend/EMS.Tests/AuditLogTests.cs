using EMS.Application.Features.AuditLogs.Handlers;
using EMS.Application.Features.AuditLogs.Queries;
using EMS.Application.Features.AuditLogs.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class AuditLogTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_auditlog_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId { get; set; }
            public string? IpAddress { get; set; } = "127.0.0.1";
            public string? UserAgent { get; set; } = "xunit-test-agent";
        }

        // ─── AuditLogger ────────────────────────────────────────────────────────

        [Fact]
        public async Task AuditLogger_LogAsync_PersistsEntryWithCurrentUserContext()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var userId = Guid.NewGuid();
            var currentUser = new FakeCurrentUserService { UserId = userId };
            var logger = new AuditLogger(repo, currentUser);

            var entityId = Guid.NewGuid();
            await logger.LogAsync("Employee", entityId, "Created",
                newValues: new { Name = "Jane Doe" }, ct: CancellationToken.None);

            var saved = Assert.Single(db.AuditLogs);
            Assert.Equal(userId, saved.UserId);
            Assert.Equal("Employee", saved.EntityName);
            Assert.Equal(entityId, saved.EntityId);
            Assert.Equal("Created", saved.Action);
            Assert.Contains("Jane Doe", saved.NewValuesJson);
            Assert.Null(saved.OldValuesJson);
            Assert.Equal("127.0.0.1", saved.IpAddress);
            Assert.Equal("xunit-test-agent", saved.UserAgent);
            Assert.NotEqual(default, saved.CreatedAtUtc);
        }

        [Fact]
        public async Task AuditLogger_LogAsync_WithoutAuthenticatedUser_LeavesUserIdNull()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var currentUser = new FakeCurrentUserService { UserId = null };
            var logger = new AuditLogger(repo, currentUser);

            await logger.LogAsync("Employee", Guid.NewGuid(), "Deleted", oldValues: new { Reason = "test" });

            var saved = Assert.Single(db.AuditLogs);
            Assert.Null(saved.UserId);
            Assert.Contains("test", saved.OldValuesJson);
        }

        // ─── AuditLogRepository ─────────────────────────────────────────────────

        [Fact]
        public async Task AuditLogRepository_GetPagedAsync_FiltersByEntityAndAction()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var employeeId = Guid.NewGuid();

            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", EntityId = employeeId, Action = "Created", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10) });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", EntityId = employeeId, Action = "Updated", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5) });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Department", EntityId = Guid.NewGuid(), Action = "Created", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var results = (await repo.GetPagedAsync(null, "Employee", null, null, null, null, 1, 20)).ToList();

            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.Equal("Employee", r.EntityName));
            // Most recent first.
            Assert.Equal("Updated", results[0].Action);
        }

        [Fact]
        public async Task AuditLogRepository_GetPagedAsync_FiltersByDateRange()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);

            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", Action = "Created", CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", Action = "Created", CreatedAtUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) });
            await repo.SaveChangesAsync();

            var results = await repo.GetPagedAsync(null, null, null, null,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 1, 20);

            Assert.Single(results);
            Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), results.Single().CreatedAtUtc);
        }

        [Fact]
        public async Task AuditLogRepository_CountAsync_MatchesFilteredResultCount()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var userId = Guid.NewGuid();

            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), UserId = userId, EntityName = "Employee", Action = "Created", CreatedAtUtc = DateTime.UtcNow });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), EntityName = "Employee", Action = "Created", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var count = await repo.CountAsync(userId, null, null, null, null, null);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task AuditLogRepository_GetForEntityAsync_ReturnsOnlyMatchingEntityHistory()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var employeeId = Guid.NewGuid();
            var otherId = Guid.NewGuid();

            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", EntityId = employeeId, Action = "Created", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1) });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", EntityId = employeeId, Action = "Updated", CreatedAtUtc = DateTime.UtcNow });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", EntityId = otherId, Action = "Created", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var history = (await repo.GetForEntityAsync("Employee", employeeId, 1, 20)).ToList();
            var count = await repo.CountForEntityAsync("Employee", employeeId);

            Assert.Equal(2, history.Count);
            Assert.Equal(2, count);
            Assert.All(history, h => Assert.Equal(employeeId, h.EntityId));
        }

        [Fact]
        public async Task AuditLogRepository_GetByIdAsync_ReturnsNullWhenMissing()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);

            var result = await repo.GetByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        // ─── Query handlers ─────────────────────────────────────────────────────

        [Fact]
        public async Task GetAuditLogsQueryHandler_ReturnsPagedResultWithDefaults()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            for (var i = 0; i < 3; i++)
                await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", Action = "Created", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i) });
            await repo.SaveChangesAsync();

            var handler = new GetAuditLogsQueryHandler(repo);
            var result = await handler.Handle(new GetAuditLogsQuery(), CancellationToken.None);

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(3, result.Data.Count());
        }

        [Fact]
        public async Task GetAuditLogByIdQueryHandler_ReturnsNull_WhenNotFound()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var handler = new GetAuditLogByIdQueryHandler(repo);

            var result = await handler.Handle(new GetAuditLogByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAuditLogsForEntityQueryHandler_ReturnsOnlyEntityHistory()
        {
            using var db = CreateDb();
            var repo = new AuditLogRepository(db);
            var employeeId = Guid.NewGuid();
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Employee", EntityId = employeeId, Action = "Created", CreatedAtUtc = DateTime.UtcNow });
            await repo.AddAsync(new AuditLog { Id = Guid.NewGuid(), EntityName = "Department", EntityId = Guid.NewGuid(), Action = "Created", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var handler = new GetAuditLogsForEntityQueryHandler(repo);
            var result = await handler.Handle(new GetAuditLogsForEntityQuery { EntityName = "Employee", EntityId = employeeId }, CancellationToken.None);

            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Employee", result.Data.Single().EntityName);
        }

        // ─── Validator ──────────────────────────────────────────────────────────

        [Fact]
        public void GetAuditLogsQueryValidator_RejectsPageSizeOutOfRange()
        {
            var validator = new GetAuditLogsQueryValidator();

            var result = validator.Validate(new GetAuditLogsQuery { PageSize = 0 });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "PageSize");
        }

        [Fact]
        public void GetAuditLogsQueryValidator_RejectsDateFromAfterDateTo()
        {
            var validator = new GetAuditLogsQueryValidator();

            var result = validator.Validate(new GetAuditLogsQuery
            {
                DateFrom = DateTime.UtcNow,
                DateTo = DateTime.UtcNow.AddDays(-1)
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public void GetAuditLogsQueryValidator_AcceptsValidQuery()
        {
            var validator = new GetAuditLogsQueryValidator();

            var result = validator.Validate(new GetAuditLogsQuery { Page = 1, PageSize = 20 });

            Assert.True(result.IsValid);
        }
    }
}

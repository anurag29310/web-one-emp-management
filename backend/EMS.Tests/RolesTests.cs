using EMS.Application.Features.Roles.Commands;
using EMS.Application.Features.Roles.Handlers;
using EMS.Application.Features.Roles.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class RolesTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_roles_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class RecordingAuditLogger : IAuditLogger
        {
            public List<(string EntityName, Guid? EntityId, string Action)> Calls { get; } = new();

            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
            {
                Calls.Add((entityName, entityId, action));
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task CreateRole_PersistsAndReturnsRole()
        {
            using var db = CreateDb();
            var repo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var handler = new CreateRoleCommandHandler(repo, audit, NullLogger<CreateRoleCommandHandler>.Instance);

            var created = await handler.Handle(new CreateRoleCommand { Name = "Auditor", Description = "Read-only compliance access" }, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("Auditor", db.Roles.Single().Name);
            Assert.Single(audit.Calls, c => c.Action == "Created" && c.EntityName == "Role");
        }

        [Fact]
        public async Task CreateRole_DuplicateName_Throws()
        {
            using var db = CreateDb();
            var repo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var handler = new CreateRoleCommandHandler(repo, audit, NullLogger<CreateRoleCommandHandler>.Instance);

            await handler.Handle(new CreateRoleCommand { Name = "Auditor" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateRoleCommand { Name = "Auditor" }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateRoleCommandValidator_RejectsDuplicateName()
        {
            using var db = CreateDb();
            var repo = new RoleRepository(db);
            await repo.AddAsync(new Role { Id = Guid.NewGuid(), Name = "Auditor", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var validator = new CreateRoleCommandValidator(repo);
            var result = await validator.ValidateAsync(new CreateRoleCommand { Name = "Auditor" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public async Task UpdateRole_ChangesFields()
        {
            using var db = CreateDb();
            var repo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateRoleCommandHandler(repo, audit, NullLogger<CreateRoleCommandHandler>.Instance);
            var updateHandler = new UpdateRoleCommandHandler(repo, audit, NullLogger<UpdateRoleCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateRoleCommand { Name = "Auditor" }, CancellationToken.None);
            var updated = await updateHandler.Handle(new UpdateRoleCommand { Id = created.Id, Name = "Auditor", Description = "Updated description" }, CancellationToken.None);

            Assert.Equal("Updated description", updated.Description);
            Assert.NotNull(updated.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeleteRole_WhenAssignedToActiveUser_Throws()
        {
            using var db = CreateDb();
            var roleRepo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateRoleCommandHandler(roleRepo, audit, NullLogger<CreateRoleCommandHandler>.Instance);
            var deleteHandler = new DeleteRoleCommandHandler(roleRepo, audit, NullLogger<DeleteRoleCommandHandler>.Instance);

            var role = await createHandler.Handle(new CreateRoleCommand { Name = "Auditor" }, CancellationToken.None);
            db.Users.Add(new User { Id = Guid.NewGuid(), UserName = "u1", Email = "u1@example.com", PasswordHash = "x", RoleId = role.Id, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                deleteHandler.Handle(new DeleteRoleCommand { Id = role.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteThenRestoreRole_TogglesIsDeleted()
        {
            using var db = CreateDb();
            var repo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateRoleCommandHandler(repo, audit, NullLogger<CreateRoleCommandHandler>.Instance);
            var deleteHandler = new DeleteRoleCommandHandler(repo, audit, NullLogger<DeleteRoleCommandHandler>.Instance);
            var restoreHandler = new RestoreRoleCommandHandler(repo, audit, NullLogger<RestoreRoleCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateRoleCommand { Name = "Auditor" }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteRoleCommand { Id = created.Id }, CancellationToken.None);
            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));

            await restoreHandler.Handle(new RestoreRoleCommand { Id = created.Id }, CancellationToken.None);
            var restored = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task RestoreRole_WhenNotDeleted_Throws()
        {
            using var db = CreateDb();
            var repo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateRoleCommandHandler(repo, audit, NullLogger<CreateRoleCommandHandler>.Instance);
            var restoreHandler = new RestoreRoleCommandHandler(repo, audit, NullLogger<RestoreRoleCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateRoleCommand { Name = "Auditor" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                restoreHandler.Handle(new RestoreRoleCommand { Id = created.Id }, CancellationToken.None));
        }
    }
}

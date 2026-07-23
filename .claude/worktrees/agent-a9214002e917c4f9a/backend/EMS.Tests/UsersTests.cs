using EMS.Application.Features.Users.Commands;
using EMS.Application.Features.Users.Handlers;
using EMS.Application.Features.Users.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Services;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class UsersTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_users_test_" + Guid.NewGuid())
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

        private const string ValidPassword = "Password@123";

        [Fact]
        public async Task CreateUser_PersistsAndReturnsUser()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var handler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), audit, NullLogger<CreateUserCommandHandler>.Instance);

            var cmd = new CreateUserCommand { UserName = "jsmith", Email = "jsmith@example.com", TemporaryPassword = ValidPassword };
            var created = await handler.Handle(cmd, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("jsmith", db.Users.Single().UserName);
            Assert.NotEqual(ValidPassword, created.PasswordHash);
            Assert.Single(audit.Calls, c => c.Action == "Created" && c.EntityName == "User");
        }

        [Fact]
        public async Task CreateUser_DuplicateUserName_Throws()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var handler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), new RecordingAuditLogger(), NullLogger<CreateUserCommandHandler>.Instance);

            await handler.Handle(new CreateUserCommand { UserName = "jsmith", Email = "a@example.com", TemporaryPassword = ValidPassword }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateUserCommand { UserName = "jsmith", Email = "b@example.com", TemporaryPassword = ValidPassword }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateUser_UnknownRole_Throws()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var handler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), new RecordingAuditLogger(), NullLogger<CreateUserCommandHandler>.Instance);

            var cmd = new CreateUserCommand { UserName = "jsmith", Email = "jsmith@example.com", TemporaryPassword = ValidPassword, RoleId = Guid.NewGuid() };

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, CancellationToken.None));
        }

        [Fact]
        public async Task CreateUserCommandValidator_RejectsWeakPassword()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var validator = new CreateUserCommandValidator(userRepo, roleRepo);

            var result = await validator.ValidateAsync(new CreateUserCommand { UserName = "jsmith", Email = "jsmith@example.com", TemporaryPassword = "weak" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "TemporaryPassword");
        }

        [Fact]
        public async Task UpdateUser_ChangesFields()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), audit, NullLogger<CreateUserCommandHandler>.Instance);
            var updateHandler = new UpdateUserCommandHandler(userRepo, roleRepo, audit, NullLogger<UpdateUserCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateUserCommand { UserName = "jsmith", Email = "jsmith@example.com", TemporaryPassword = ValidPassword }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateUserCommand { Id = created.Id, UserName = "jsmith2", Email = "jsmith2@example.com" }, CancellationToken.None);

            Assert.Equal("jsmith2", updated.UserName);
            Assert.NotNull(updated.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeactivateUser_RevokesActiveRefreshTokens()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var authRepo = new AuthRepository(db);
            var audit = new RecordingAuditLogger();

            var user = new User { Id = Guid.NewGuid(), UserName = "jsmith", Email = "jsmith@example.com", PasswordHash = "x", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            db.Users.Add(user);
            db.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, Token = "tok1", ExpiresAtUtc = DateTime.UtcNow.AddDays(1), IsRevoked = false, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var handler = new UpdateUserStatusCommandHandler(userRepo, authRepo, audit, NullLogger<UpdateUserStatusCommandHandler>.Instance);
            await handler.Handle(new UpdateUserStatusCommand { Id = user.Id, IsActive = false }, CancellationToken.None);

            Assert.True(db.RefreshTokens.Single().IsRevoked);
            Assert.False(db.Users.Single().IsActive);
        }

        [Fact]
        public async Task DeleteThenRestoreUser_TogglesIsDeleted()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var authRepo = new AuthRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), audit, NullLogger<CreateUserCommandHandler>.Instance);
            var deleteHandler = new DeleteUserCommandHandler(userRepo, authRepo, audit, NullLogger<DeleteUserCommandHandler>.Instance);
            var restoreHandler = new RestoreUserCommandHandler(userRepo, audit, NullLogger<RestoreUserCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateUserCommand { UserName = "jsmith", Email = "jsmith@example.com", TemporaryPassword = ValidPassword }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteUserCommand { Id = created.Id }, CancellationToken.None);
            Assert.Null(await userRepo.GetByIdAsync(created.Id, CancellationToken.None));

            await restoreHandler.Handle(new RestoreUserCommand { Id = created.Id }, CancellationToken.None);
            var restored = await userRepo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task RestoreUser_WhenNotDeleted_Throws()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), audit, NullLogger<CreateUserCommandHandler>.Instance);
            var restoreHandler = new RestoreUserCommandHandler(userRepo, audit, NullLogger<RestoreUserCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateUserCommand { UserName = "jsmith", Email = "jsmith@example.com", TemporaryPassword = ValidPassword }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                restoreHandler.Handle(new RestoreUserCommand { Id = created.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task DeletedUser_CannotBeFoundByAuthRepository()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var authRepo = new AuthRepository(db);
            var audit = new RecordingAuditLogger();

            var user = new User { Id = Guid.NewGuid(), UserName = "jsmith", Email = "jsmith@example.com", PasswordHash = "x", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var deleteHandler = new DeleteUserCommandHandler(userRepo, authRepo, audit, NullLogger<DeleteUserCommandHandler>.Instance);
            await deleteHandler.Handle(new DeleteUserCommand { Id = user.Id }, CancellationToken.None);

            Assert.Null(await authRepo.GetByUsernameOrEmailAsync("jsmith", CancellationToken.None));
        }

        [Fact]
        public async Task GetUsersQuery_FiltersByIsActiveAndExcludesDeletedByDefault()
        {
            using var db = CreateDb();
            var userRepo = new UserRepository(db);
            var roleRepo = new RoleRepository(db);
            var authRepo = new AuthRepository(db);
            var audit = new RecordingAuditLogger();
            var createHandler = new CreateUserCommandHandler(userRepo, roleRepo, new PasswordHashService(), audit, NullLogger<CreateUserCommandHandler>.Instance);
            var deleteHandler = new DeleteUserCommandHandler(userRepo, authRepo, audit, NullLogger<DeleteUserCommandHandler>.Instance);

            var active = await createHandler.Handle(new CreateUserCommand { UserName = "active1", Email = "active1@example.com", TemporaryPassword = ValidPassword, IsActive = true }, CancellationToken.None);
            var inactive = await createHandler.Handle(new CreateUserCommand { UserName = "inactive1", Email = "inactive1@example.com", TemporaryPassword = ValidPassword, IsActive = false }, CancellationToken.None);
            var deleted = await createHandler.Handle(new CreateUserCommand { UserName = "deleted1", Email = "deleted1@example.com", TemporaryPassword = ValidPassword }, CancellationToken.None);
            await deleteHandler.Handle(new DeleteUserCommand { Id = deleted.Id }, CancellationToken.None);

            var activeOnly = await userRepo.GetAllAsync(includeDeleted: false, roleId: null, isActive: true, ct: CancellationToken.None);
            Assert.Single(activeOnly, u => u.Id == active.Id);

            var allIncludingDeleted = await userRepo.GetAllAsync(includeDeleted: true, ct: CancellationToken.None);
            Assert.Equal(3, allIncludingDeleted.Count());
        }
    }
}

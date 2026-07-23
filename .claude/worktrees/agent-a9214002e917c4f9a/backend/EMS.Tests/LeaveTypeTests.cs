using EMS.Application.Features.Leave.Commands;
using EMS.Application.Features.Leave.Handlers;
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
    public class LeaveTypeTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_leavetype_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateLeaveType_Succeeds()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var handler = new CreateLeaveTypeCommandHandler(repo, NullLogger<CreateLeaveTypeCommandHandler>.Instance);

            var result = await handler.Handle(new CreateLeaveTypeCommand { Name = "Sick Leave", Code = "SICK", IsPaid = true, AnnualEntitlementDays = 10 }, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Sick Leave", result.Name);
        }

        [Fact]
        public async Task CreateLeaveType_DuplicateCode_Throws()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var handler = new CreateLeaveTypeCommandHandler(repo, NullLogger<CreateLeaveTypeCommandHandler>.Instance);

            await handler.Handle(new CreateLeaveTypeCommand { Name = "Sick Leave", Code = "SICK" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateLeaveTypeCommand { Name = "Another Sick Leave", Code = "SICK" }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteLeaveType_SoftDeletes_AndHidesFromLookups()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var createHandler = new CreateLeaveTypeCommandHandler(repo, NullLogger<CreateLeaveTypeCommandHandler>.Instance);
            var created = await createHandler.Handle(new CreateLeaveTypeCommand { Name = "Casual Leave" }, CancellationToken.None);

            var deleteHandler = new DeleteLeaveTypeCommandHandler(repo, NullLogger<DeleteLeaveTypeCommandHandler>.Instance);
            await deleteHandler.Handle(new DeleteLeaveTypeCommand { Id = created.Id }, CancellationToken.None);

            Assert.Null(await repo.GetLeaveTypeByIdAsync(created.Id, CancellationToken.None));
            var stored = await repo.GetLeaveTypeByIdIncludingDeletedAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.True(stored!.IsDeleted);
        }

        [Fact]
        public async Task DeleteLeaveType_FreesUpItsCodeForReuse()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var createHandler = new CreateLeaveTypeCommandHandler(repo, NullLogger<CreateLeaveTypeCommandHandler>.Instance);
            var created = await createHandler.Handle(new CreateLeaveTypeCommand { Name = "Casual Leave", Code = "CASUAL" }, CancellationToken.None);

            var deleteHandler = new DeleteLeaveTypeCommandHandler(repo, NullLogger<DeleteLeaveTypeCommandHandler>.Instance);
            await deleteHandler.Handle(new DeleteLeaveTypeCommand { Id = created.Id }, CancellationToken.None);

            // Code should be free again since the original leave type using it is soft-deleted.
            var recreated = await createHandler.Handle(new CreateLeaveTypeCommand { Name = "Casual Leave v2", Code = "CASUAL" }, CancellationToken.None);
            Assert.NotEqual(created.Id, recreated.Id);
        }

        [Fact]
        public async Task RestoreLeaveType_UndeletesRecordAndItReappearsInLookups()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var createHandler = new CreateLeaveTypeCommandHandler(repo, NullLogger<CreateLeaveTypeCommandHandler>.Instance);
            var created = await createHandler.Handle(new CreateLeaveTypeCommand { Name = "Casual Leave" }, CancellationToken.None);

            var deleteHandler = new DeleteLeaveTypeCommandHandler(repo, NullLogger<DeleteLeaveTypeCommandHandler>.Instance);
            await deleteHandler.Handle(new DeleteLeaveTypeCommand { Id = created.Id }, CancellationToken.None);

            var restoreHandler = new RestoreLeaveTypeCommandHandler(repo, NullLogger<RestoreLeaveTypeCommandHandler>.Instance);
            await restoreHandler.Handle(new RestoreLeaveTypeCommand { Id = created.Id }, CancellationToken.None);

            var restored = await repo.GetLeaveTypeByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task RestoreLeaveType_WhenNotDeleted_Throws()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var createHandler = new CreateLeaveTypeCommandHandler(repo, NullLogger<CreateLeaveTypeCommandHandler>.Instance);
            var created = await createHandler.Handle(new CreateLeaveTypeCommand { Name = "Casual Leave" }, CancellationToken.None);

            var restoreHandler = new RestoreLeaveTypeCommandHandler(repo, NullLogger<RestoreLeaveTypeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                restoreHandler.Handle(new RestoreLeaveTypeCommand { Id = created.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task RestoreLeaveType_UnknownId_Throws()
        {
            using var db = CreateDb();
            var repo = new LeaveRepository(db);
            var restoreHandler = new RestoreLeaveTypeCommandHandler(repo, NullLogger<RestoreLeaveTypeCommandHandler>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                restoreHandler.Handle(new RestoreLeaveTypeCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        }
    }
}

using EMS.Application.Features.Tasks;
using EMS.Application.Features.Tasks.Handlers;
using EMS.Application.Features.Tasks.Validators;
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
    public class TaskManagementTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_task_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static Mock<IAuthRepository> AuthRepoFor(Guid userId, Guid? employeeId)
        {
            var mock = new Mock<IAuthRepository>();
            mock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = userId, EmployeeId = employeeId, UserName = "u", Email = "u@test.com", PasswordHash = "x" });
            return mock;
        }

        // TaskItem.AssignedEmployee is a required (INNER JOIN) navigation, so — same gotcha
        // documented in EmployeeTests.cs — Include(t => t.AssignedEmployee) silently drops any row
        // whose AssignedEmployeeId doesn't resolve to a real Employee under the InMemory provider
        // (a real Postgres FK constraint would prevent that from ever happening in production).
        // Every test therefore seeds a real Employee row rather than pointing at a bare Guid.
        private static async Task<Guid> SeedEmployeeAsync(ApplicationDbContext db)
        {
            var designationId = Guid.NewGuid();
            var officeLocationId = Guid.NewGuid();
            db.Designations.Add(new Designation { Id = designationId, Name = "Designation-" + designationId, Code = "DSG-" + designationId.ToString("N")[..8], CreatedAtUtc = DateTime.UtcNow });
            db.OfficeLocations.Add(new OfficeLocation { Id = officeLocationId, Name = "Location-" + officeLocationId, Code = "LOC-" + officeLocationId.ToString("N")[..8], City = "City", Country = "Country", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow });

            var employeeId = Guid.NewGuid();
            db.Employees.Add(new Employee
            {
                Id = employeeId,
                EmployeeCode = "EMP-" + employeeId.ToString("N")[..8],
                FirstName = "Test",
                LastName = "Employee",
                Email = $"{employeeId:N}@test.local",
                JoinDate = DateTime.UtcNow.Date,
                IsActive = true,
                DesignationId = designationId,
                OfficeLocationId = officeLocationId,
                EmploymentStatus = "Active"
            });

            await db.SaveChangesAsync();
            return employeeId;
        }

        private static CreateTaskCommand ValidCreateCommand(Guid assignedEmployeeId, Guid requestingUserId) => new()
        {
            Title = "Deliver quarterly report",
            AssignedEmployeeId = assignedEmployeeId,
            Priority = TaskItemPriority.High,
            RequestingUserId = requestingUserId
        };

        [Fact]
        public async Task CreateTask_PersistsWithGeneratedNumberAndAssignedStatus()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var handler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);

            var created = await handler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            Assert.StartsWith("TSK-", created.TaskNumber);
            Assert.Equal(TaskItemStatus.Assigned, created.Status);
            Assert.Equal(adminId, created.AssignedByUserId);
            Assert.Equal(adminId, created.CreatedBy);
        }

        [Fact]
        public async Task UpdateTask_ChangesFields_ButRejectedWhenCompleted()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var updateHandler = new UpdateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<UpdateTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateTaskCommand
            {
                Id = created.Id,
                Title = "Deliver quarterly report (revised)",
                Priority = TaskItemPriority.Critical,
                RequestingUserId = adminId
            }, CancellationToken.None);

            Assert.Equal("Deliver quarterly report (revised)", updated.Title);
            Assert.Equal(TaskItemPriority.Critical, updated.Priority);

            // Force the task into Completed, then updates must be rejected.
            var entity = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            entity!.Status = TaskItemStatus.Completed;
            await repo.UpdateAsync(entity, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                updateHandler.Handle(new UpdateTaskCommand { Id = created.Id, Title = "Should fail", Priority = TaskItemPriority.Low, RequestingUserId = adminId }, CancellationToken.None));
        }

        [Fact]
        public async Task ReassignTask_ChangesAssigneeAndResetsStatusToAssigned()
        {
            using var db = CreateDb();
            var originalEmployeeId = await SeedEmployeeAsync(db);
            var newEmployeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var reassignHandler = new ReassignTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<ReassignTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(originalEmployeeId, adminId), CancellationToken.None);

            var entity = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            entity!.Status = TaskItemStatus.InProgress;
            await repo.UpdateAsync(entity, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            var reassigned = await reassignHandler.Handle(new ReassignTaskCommand { Id = created.Id, AssignedEmployeeId = newEmployeeId, RequestingUserId = adminId }, CancellationToken.None);

            Assert.Equal(newEmployeeId, reassigned.AssignedEmployeeId);
            Assert.Equal(TaskItemStatus.Assigned, reassigned.Status);
        }

        [Fact]
        public async Task CancelTask_SetsCancelled_ButRejectedWhenAlreadyCompleted()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var cancelHandler = new CancelTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CancelTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);
            await cancelHandler.Handle(new CancelTaskCommand { Id = created.Id }, CancellationToken.None);

            var cancelled = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(TaskItemStatus.Cancelled, cancelled!.Status);

            cancelled.Status = TaskItemStatus.Completed;
            await repo.UpdateAsync(cancelled, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                cancelHandler.Handle(new CancelTaskCommand { Id = created.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task AcceptTask_AssigneeCanAccept_NonAssigneeIsRejected()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            var otherAuthRepo = AuthRepoFor(otherUserId, Guid.NewGuid());
            var acceptHandlerAsOther = new AcceptTaskCommandHandler(repo, otherAuthRepo.Object, new RecordingAuditLogger(), NullLogger<AcceptTaskCommandHandler>.Instance);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                acceptHandlerAsOther.Handle(new AcceptTaskCommand { Id = created.Id, RequestingUserId = otherUserId, IsPrivileged = false }, CancellationToken.None));

            var assigneeUserId = Guid.NewGuid();
            var assigneeAuthRepo = AuthRepoFor(assigneeUserId, employeeId);
            var acceptHandler = new AcceptTaskCommandHandler(repo, assigneeAuthRepo.Object, new RecordingAuditLogger(), NullLogger<AcceptTaskCommandHandler>.Instance);
            await acceptHandler.Handle(new AcceptTaskCommand { Id = created.Id, RequestingUserId = assigneeUserId, IsPrivileged = false }, CancellationToken.None);

            var accepted = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(TaskItemStatus.Accepted, accepted!.Status);
        }

        [Fact]
        public async Task RejectTask_MovesToRejectedStatus_OnlyFromAssigned()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var rejectHandler = new RejectTaskCommandHandler(repo, AuthRepoFor(adminId, employeeId).Object, new RecordingAuditLogger(), NullLogger<RejectTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            await rejectHandler.Handle(new RejectTaskCommand { Id = created.Id, Reason = "Overloaded this sprint", RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);

            var rejected = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(TaskItemStatus.Rejected, rejected!.Status);
            Assert.Contains("Overloaded", rejected.Notes);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                rejectHandler.Handle(new RejectTaskCommand { Id = created.Id, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None));
        }

        [Fact]
        public async Task StartAndCompleteTask_FollowsStatusWorkflow()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var authRepo = AuthRepoFor(adminId, employeeId).Object;
            var acceptHandler = new AcceptTaskCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<AcceptTaskCommandHandler>.Instance);
            var startHandler = new StartTaskCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<StartTaskCommandHandler>.Instance);
            var completeHandler = new CompleteTaskCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<CompleteTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            // Cannot start before Accepted.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                startHandler.Handle(new StartTaskCommand { Id = created.Id, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None));

            await acceptHandler.Handle(new AcceptTaskCommand { Id = created.Id, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);
            await startHandler.Handle(new StartTaskCommand { Id = created.Id, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);

            var inProgress = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(TaskItemStatus.InProgress, inProgress!.Status);

            await completeHandler.Handle(new CompleteTaskCommand { Id = created.Id, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);

            var completed = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.Equal(TaskItemStatus.Completed, completed!.Status);
            Assert.NotNull(completed.CompletedAtUtc);
        }

        [Fact]
        public async Task UpdateTaskProgress_TogglesBetweenInProgressAndOnHold()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var authRepo = AuthRepoFor(adminId, employeeId).Object;
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var progressHandler = new UpdateTaskProgressCommandHandler(repo, authRepo, new RecordingAuditLogger(), NullLogger<UpdateTaskProgressCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);
            var entity = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            entity!.Status = TaskItemStatus.InProgress;
            await repo.UpdateAsync(entity, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            await progressHandler.Handle(new UpdateTaskProgressCommand { Id = created.Id, Status = TaskItemStatus.OnHold, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);
            Assert.Equal(TaskItemStatus.OnHold, (await repo.GetByIdAsync(created.Id, CancellationToken.None))!.Status);

            await progressHandler.Handle(new UpdateTaskProgressCommand { Id = created.Id, Status = TaskItemStatus.InProgress, RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);
            Assert.Equal(TaskItemStatus.InProgress, (await repo.GetByIdAsync(created.Id, CancellationToken.None))!.Status);
        }

        [Fact]
        public async Task AddTaskComment_Persists_ButRejectedWhenCompleted()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var authRepo = AuthRepoFor(adminId, employeeId).Object;
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);
            var commentHandler = new AddTaskCommentCommandHandler(repo, authRepo, NullLogger<AddTaskCommentCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            var comment = await commentHandler.Handle(new AddTaskCommentCommand { TaskId = created.Id, Comment = "Site visit scheduled for Friday.", RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None);
            Assert.Equal("Site visit scheduled for Friday.", comment.Comment);

            var stored = await repo.GetCommentsAsync(created.Id, CancellationToken.None);
            Assert.Single(stored);

            var entity = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            entity!.Status = TaskItemStatus.Completed;
            await repo.UpdateAsync(entity, CancellationToken.None);
            await repo.SaveChangesAsync(CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                commentHandler.Handle(new AddTaskCommentCommand { TaskId = created.Id, Comment = "Too late", RequestingUserId = adminId, IsPrivileged = true }, CancellationToken.None));
        }

        [Fact]
        public async Task GetTasksQuery_NonPrivilegedCaller_IsScopedToOwnTasksRegardlessOfFilter()
        {
            using var db = CreateDb();
            var employeeAId = await SeedEmployeeAsync(db);
            var employeeBId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);

            await createHandler.Handle(ValidCreateCommand(employeeAId, adminId), CancellationToken.None);
            await createHandler.Handle(ValidCreateCommand(employeeBId, adminId), CancellationToken.None);

            var callerUserId = Guid.NewGuid();
            var authRepo = AuthRepoFor(callerUserId, employeeAId).Object;
            var queryHandler = new GetTasksQueryHandler(repo, authRepo);

            // Even though this caller explicitly asks for employee B's tasks, a non-privileged
            // caller must only ever see their own.
            var result = await queryHandler.Handle(new GetTasksQuery { AssignedEmployeeId = employeeBId, RequestingUserId = callerUserId, IsPrivileged = false }, CancellationToken.None);

            Assert.Single(result.Data);
            Assert.Equal(employeeAId, result.Data.Single().AssignedEmployeeId);
        }

        [Fact]
        public async Task GetTaskByIdQuery_ReturnsNullForNonAssigneeNonPrivilegedCaller()
        {
            using var db = CreateDb();
            var employeeId = await SeedEmployeeAsync(db);
            var repo = new TaskRepository(db);
            var adminId = Guid.NewGuid();
            var createHandler = new CreateTaskCommandHandler(repo, new RecordingAuditLogger(), NullLogger<CreateTaskCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand(employeeId, adminId), CancellationToken.None);

            var otherUserId = Guid.NewGuid();
            var authRepo = AuthRepoFor(otherUserId, Guid.NewGuid()).Object;
            var getHandler = new GetTaskByIdQueryHandler(repo, authRepo);

            var result = await getHandler.Handle(new GetTaskByIdQuery { Id = created.Id, RequestingUserId = otherUserId, IsPrivileged = false }, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateTaskCommandValidator_RejectsMissingEmployeeAndInactiveClient()
        {
            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

            var inactiveClientId = Guid.NewGuid();
            var clientRepo = new Mock<IClientRepository>();
            clientRepo.Setup(r => r.GetByIdAsync(inactiveClientId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Client { Id = inactiveClientId, IsActive = false, ClientName = "X", CompanyName = "X", ContactPerson = "X", MobileNumber = "X", Email = "x@x.com", AddressLine1 = "X", City = "X", Country = "X", PostalCode = "X" });

            var validator = new CreateTaskCommandValidator(employeeRepo.Object, clientRepo.Object);

            var result = await validator.ValidateAsync(new CreateTaskCommand
            {
                Title = "Visit client",
                AssignedEmployeeId = Guid.NewGuid(),
                ClientId = inactiveClientId,
                RequestingUserId = Guid.NewGuid()
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "AssignedEmployeeId");
            Assert.Contains(result.Errors, e => e.PropertyName == "ClientId");
        }
    }
}

using EMS.Application.Features.Assets.Commands;
using EMS.Application.Features.Assets.Handlers;
using EMS.Application.Features.Assets.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class AssetManagementTests
    {
        private class RecordingAuditLogger : IAuditLogger
        {
            public Task LogAsync(string entityName, Guid? entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken ct = default)
                => Task.CompletedTask;
        }

        private static readonly Guid TestCompanyId = Guid.NewGuid();

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public Guid? CompanyId => TestCompanyId;
            public string? IpAddress => null;
            public string? UserAgent => null;
        }

        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_asset_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<Employee> SeedEmployeeAsync(ApplicationDbContext db)
        {
            var designationId = Guid.NewGuid();
            var officeLocationId = Guid.NewGuid();
            db.Designations.Add(new Designation { Id = designationId, Name = "Designation-" + designationId, Code = "DSG-" + designationId.ToString("N")[..8], CreatedAtUtc = DateTime.UtcNow });
            db.OfficeLocations.Add(new OfficeLocation { Id = officeLocationId, Name = "Location-" + officeLocationId, Code = "LOC-" + officeLocationId.ToString("N")[..8], City = "City", Country = "Country", TimeZoneId = "UTC", CreatedAtUtc = DateTime.UtcNow });

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeCode = "EMP-" + Guid.NewGuid().ToString("N")[..8],
                FirstName = "Test",
                LastName = "Employee",
                JoinDate = DateTime.UtcNow.Date,
                IsActive = true,
                DesignationId = designationId,
                OfficeLocationId = officeLocationId
            };
            db.Employees.Add(employee);

            await db.SaveChangesAsync();
            return employee;
        }

        private static CreateAssetCommand ValidCreateCommand() => new()
        {
            Category = "Laptop",
            Brand = "Dell",
            Model = "Latitude 5440",
            SerialNumber = "SN-" + Guid.NewGuid().ToString("N")[..10]
        };

        [Fact]
        public async Task CreateAsset_PersistsWithGeneratedTagAndAvailableStatus()
        {
            using var db = CreateDb();
            var repo = new AssetRepository(db);
            var handler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);

            var id = await handler.Handle(ValidCreateCommand(), CancellationToken.None);

            var asset = await repo.GetByIdAsync(id, CancellationToken.None);
            Assert.NotNull(asset);
            Assert.StartsWith("AST-", asset!.AssetTag);
            Assert.Equal(AssetStatus.Available, asset.Status);
        }

        [Fact]
        public async Task AssignAsset_FromAvailable_CreatesAssignmentAndSetsStatusAssigned()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            var requestingUserId = Guid.NewGuid();
            var assignmentId = await assignHandler.Handle(new AssignAssetCommand
            {
                AssetId = assetId,
                EmployeeId = employee.Id,
                RequestingUserId = requestingUserId
            }, CancellationToken.None);

            var asset = await repo.GetByIdAsync(assetId, CancellationToken.None);
            Assert.Equal(AssetStatus.Assigned, asset!.Status);

            var assignment = await repo.GetAssignmentByIdAsync(assignmentId, CancellationToken.None);
            Assert.Equal(employee.Id, assignment!.EmployeeId);
            Assert.Equal(requestingUserId, assignment.AssignedByUserId);
            Assert.Null(assignment.ReturnedDate);
        }

        [Fact]
        public async Task AssignAsset_WhenAlreadyAssigned_Throws()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var otherEmployee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            await assignHandler.Handle(new AssignAssetCommand { AssetId = assetId, EmployeeId = employee.Id, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => assignHandler.Handle(new AssignAssetCommand
            {
                AssetId = assetId,
                EmployeeId = otherEmployee.Id,
                RequestingUserId = Guid.NewGuid()
            }, CancellationToken.None));
        }

        [Fact]
        public async Task ReturnAsset_ClosesAssignmentAndSetsResultingStatus()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);
            var returnHandler = new ReturnAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ReturnAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            var assignmentId = await assignHandler.Handle(new AssignAssetCommand { AssetId = assetId, EmployeeId = employee.Id, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);

            await returnHandler.Handle(new ReturnAssetCommand
            {
                Id = assignmentId,
                ConditionAtReturn = "Minor scratches",
                ResultingAssetStatus = AssetStatus.UnderRepair
            }, CancellationToken.None);

            var assignment = await repo.GetAssignmentByIdAsync(assignmentId, CancellationToken.None);
            Assert.NotNull(assignment!.ReturnedDate);
            Assert.Equal("Minor scratches", assignment.ConditionAtReturn);

            var asset = await repo.GetByIdAsync(assetId, CancellationToken.None);
            Assert.Equal(AssetStatus.UnderRepair, asset!.Status);
        }

        [Fact]
        public async Task ReturnAsset_AlreadyReturned_Throws()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);
            var returnHandler = new ReturnAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ReturnAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            var assignmentId = await assignHandler.Handle(new AssignAssetCommand { AssetId = assetId, EmployeeId = employee.Id, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);
            await returnHandler.Handle(new ReturnAssetCommand { Id = assignmentId }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => returnHandler.Handle(new ReturnAssetCommand { Id = assignmentId }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAssetStatus_WhileAssigned_Throws()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);
            var statusHandler = new UpdateAssetStatusCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<UpdateAssetStatusCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            await assignHandler.Handle(new AssignAssetCommand { AssetId = assetId, EmployeeId = employee.Id, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => statusHandler.Handle(new UpdateAssetStatusCommand { Id = assetId, Status = AssetStatus.Retired }, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAssetStatus_ToAssignedDirectly_Throws()
        {
            using var db = CreateDb();
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var statusHandler = new UpdateAssetStatusCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<UpdateAssetStatusCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => statusHandler.Handle(new UpdateAssetStatusCommand { Id = assetId, Status = AssetStatus.Assigned }, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsset_WhileAssigned_Throws()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);
            var deleteHandler = new DeleteAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeleteAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            await assignHandler.Handle(new AssignAssetCommand { AssetId = assetId, EmployeeId = employee.Id, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() => deleteHandler.Handle(new DeleteAssetCommand { Id = assetId }, CancellationToken.None));
        }

        [Fact]
        public async Task Asset_SoftDeleteThenRestore_ExcludesThenReincludesFromQueries()
        {
            using var db = CreateDb();
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var deleteHandler = new DeleteAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeleteAssetCommandHandler>.Instance);
            var restoreHandler = new RestoreAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<RestoreAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            await deleteHandler.Handle(new DeleteAssetCommand { Id = assetId }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(assetId, CancellationToken.None));

            await restoreHandler.Handle(new RestoreAssetCommand { Id = assetId }, CancellationToken.None);
            var restored = await repo.GetByIdAsync(assetId, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task AssignAssetCommandValidator_RejectsNonexistentEmployee()
        {
            var employeeRepo = new Mock<IEmployeeRepository>();
            employeeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

            var validator = new AssignAssetCommandValidator(employeeRepo.Object);
            var result = await validator.ValidateAsync(new AssignAssetCommand { AssetId = Guid.NewGuid(), EmployeeId = Guid.NewGuid() });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "EmployeeId");
        }

        [Fact]
        public async Task FullLifecycle_CreateAssignReturnRetire()
        {
            using var db = CreateDb();
            var employee = await SeedEmployeeAsync(db);
            var repo = new AssetRepository(db);
            var createHandler = new CreateAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateAssetCommandHandler>.Instance);
            var assignHandler = new AssignAssetCommandHandler(repo, new RecordingAuditLogger(), NullLogger<AssignAssetCommandHandler>.Instance);
            var returnHandler = new ReturnAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ReturnAssetCommandHandler>.Instance);
            var statusHandler = new UpdateAssetStatusCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<UpdateAssetStatusCommandHandler>.Instance);
            var deleteHandler = new DeleteAssetCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeleteAssetCommandHandler>.Instance);

            var assetId = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
            var assignmentId = await assignHandler.Handle(new AssignAssetCommand { AssetId = assetId, EmployeeId = employee.Id, RequestingUserId = Guid.NewGuid() }, CancellationToken.None);
            await returnHandler.Handle(new ReturnAssetCommand { Id = assignmentId, ResultingAssetStatus = AssetStatus.Available }, CancellationToken.None);
            await statusHandler.Handle(new UpdateAssetStatusCommand { Id = assetId, Status = AssetStatus.Retired }, CancellationToken.None);

            var asset = await repo.GetByIdAsync(assetId, CancellationToken.None);
            Assert.Equal(AssetStatus.Retired, asset!.Status);

            // A retired asset (not assigned) can now be deleted.
            await deleteHandler.Handle(new DeleteAssetCommand { Id = assetId }, CancellationToken.None);
            Assert.Null(await repo.GetByIdAsync(assetId, CancellationToken.None));

            var history = await repo.GetAssignmentsByEmployeeAsync(employee.Id, CancellationToken.None);
            Assert.Single(history);
        }
    }
}

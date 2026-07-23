using EMS.Application.Features.Departments;
using EMS.Application.Features.Departments.Handlers;
using EMS.Application.Features.Departments.Validators;
using EMS.Persistence.Context;
using EMS.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMS.Tests
{
    public class DepartmentTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_department_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task CreateDepartment_PersistsAndReturnsDepartment()
        {
            using var db = CreateDb();
            var repo = new DepartmentRepository(db);
            var handler = new CreateDepartmentCommandHandler(repo, NullLogger<CreateDepartmentCommandHandler>.Instance);

            var cmd = new CreateDepartmentCommand { Name = "Human Resources", Code = "HR" };
            var created = await handler.Handle(cmd, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("Human Resources", db.Departments.Single().Name);
        }

        [Fact]
        public async Task CreateDepartment_DuplicateName_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new DepartmentRepository(db);
            var handler = new CreateDepartmentCommandHandler(repo, NullLogger<CreateDepartmentCommandHandler>.Instance);

            await handler.Handle(new CreateDepartmentCommand { Name = "Finance", Code = "FIN" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateDepartmentCommand { Name = "Finance", Code = "FIN2" }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateDepartmentCommandValidator_RejectsDuplicateName()
        {
            using var db = CreateDb();
            var repo = new DepartmentRepository(db);
            await repo.AddAsync(new EMS.Domain.Entities.Department { Id = Guid.NewGuid(), Name = "Engineering", Code = "ENG", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var validator = new CreateDepartmentCommandValidator(repo);
            var result = await validator.ValidateAsync(new CreateDepartmentCommand { Name = "Engineering", Code = "ENG2" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public async Task UpdateDepartment_ChangesFields()
        {
            using var db = CreateDb();
            var repo = new DepartmentRepository(db);
            var createHandler = new CreateDepartmentCommandHandler(repo, NullLogger<CreateDepartmentCommandHandler>.Instance);
            var updateHandler = new UpdateDepartmentCommandHandler(repo, NullLogger<UpdateDepartmentCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateDepartmentCommand { Name = "Sales", Code = "SAL" }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateDepartmentCommand { Id = created.Id, Name = "Sales & Marketing", Code = "SAL" }, CancellationToken.None);

            Assert.Equal("Sales & Marketing", updated.Name);
            Assert.NotNull(updated.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeleteThenRestoreDepartment_TogglesIsDeleted()
        {
            using var db = CreateDb();
            var repo = new DepartmentRepository(db);
            var createHandler = new CreateDepartmentCommandHandler(repo, NullLogger<CreateDepartmentCommandHandler>.Instance);
            var deleteHandler = new DeleteDepartmentCommandHandler(repo, NullLogger<DeleteDepartmentCommandHandler>.Instance);
            var restoreHandler = new RestoreDepartmentCommandHandler(repo, NullLogger<RestoreDepartmentCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateDepartmentCommand { Name = "Legal", Code = "LEG" }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteDepartmentCommand { Id = created.Id }, CancellationToken.None);
            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));

            await restoreHandler.Handle(new RestoreDepartmentCommand { Id = created.Id }, CancellationToken.None);
            var restored = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task RestoreDepartment_WhenNotDeleted_Throws()
        {
            using var db = CreateDb();
            var repo = new DepartmentRepository(db);
            var createHandler = new CreateDepartmentCommandHandler(repo, NullLogger<CreateDepartmentCommandHandler>.Instance);
            var restoreHandler = new RestoreDepartmentCommandHandler(repo, NullLogger<RestoreDepartmentCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateDepartmentCommand { Name = "Marketing", Code = "MKT" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                restoreHandler.Handle(new RestoreDepartmentCommand { Id = created.Id }, CancellationToken.None));
        }
    }
}

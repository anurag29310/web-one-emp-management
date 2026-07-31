using EMS.Application.Features.Designations;
using EMS.Application.Features.Designations.Handlers;
using EMS.Application.Features.Designations.Validators;
using EMS.Application.Interfaces;
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
    public class DesignationTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_designation_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private static readonly Guid TestCompanyId = Guid.NewGuid();

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public Guid? CompanyId => TestCompanyId;
            public string? IpAddress => null;
            public string? UserAgent => null;
        }

        [Fact]
        public async Task CreateDesignation_PersistsAndReturnsDesignation()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            var handler = new CreateDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateDesignationCommandHandler>.Instance);

            var cmd = new CreateDesignationCommand { Name = "Software Engineer", Code = "SE", Level = 2 };
            var created = await handler.Handle(cmd, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.NotNull(created.CreatedBy);
            Assert.Equal("Software Engineer", db.Designations.Single().Name);
        }

        [Fact]
        public async Task CreateDesignation_DuplicateName_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            var handler = new CreateDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateDesignationCommandHandler>.Instance);

            await handler.Handle(new CreateDesignationCommand { Name = "Manager", Code = "MGR" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateDesignationCommand { Name = "Manager", Code = "MGR2" }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateDesignation_DuplicateCode_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            var handler = new CreateDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateDesignationCommandHandler>.Instance);

            await handler.Handle(new CreateDesignationCommand { Name = "Director", Code = "DIR" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateDesignationCommand { Name = "Sr Director", Code = "DIR" }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateDesignationCommandValidator_RejectsDuplicateName()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            await repo.AddAsync(new EMS.Domain.Entities.Designation { Id = Guid.NewGuid(), CompanyId = TestCompanyId, Name = "Analyst", Code = "ANL", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var validator = new CreateDesignationCommandValidator(repo, new FakeCurrentUserService());
            var result = await validator.ValidateAsync(new CreateDesignationCommand { Name = "Analyst", Code = "ANL2" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        }

        [Fact]
        public async Task UpdateDesignation_ChangesFields()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            var createHandler = new CreateDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateDesignationCommandHandler>.Instance);
            var updateHandler = new UpdateDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<UpdateDesignationCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateDesignationCommand { Name = "Consultant", Code = "CON", Level = 1 }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateDesignationCommand { Id = created.Id, Name = "Senior Consultant", Code = "CON", Level = 2 }, CancellationToken.None);

            Assert.Equal("Senior Consultant", updated.Name);
            Assert.Equal(2, updated.Level);
            Assert.NotNull(updated.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeleteDesignation_SoftDeletesAndHidesFromGetById()
        {
            using var db = CreateDb();
            var repo = new DesignationRepository(db);
            var createHandler = new CreateDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateDesignationCommandHandler>.Instance);
            var deleteHandler = new DeleteDesignationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<DeleteDesignationCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateDesignationCommand { Name = "Intern", Code = "INT" }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteDesignationCommand { Id = created.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(created.Id, TestCompanyId, CancellationToken.None));
            var stillThere = await repo.GetByIdIncludingDeletedAsync(created.Id, TestCompanyId, CancellationToken.None);
            Assert.NotNull(stillThere);
            Assert.True(stillThere!.IsDeleted);
        }
    }
}

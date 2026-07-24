using EMS.Application.Features.Teams;
using EMS.Application.Features.Teams.Handlers;
using EMS.Application.Features.Teams.Validators;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
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
    public class TeamTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_team_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public string? IpAddress => null;
            public string? UserAgent => null;
        }

        private static async Task<Department> SeedDepartmentAsync(ApplicationDbContext db, string suffix)
        {
            var dept = new Department { Id = Guid.NewGuid(), Name = "Dept-" + suffix, Code = "D" + suffix, CreatedAtUtc = DateTime.UtcNow };
            db.Departments.Add(dept);
            await db.SaveChangesAsync();
            return dept;
        }

        [Fact]
        public async Task CreateTeam_PersistsAndReturnsTeam()
        {
            using var db = CreateDb();
            var dept = await SeedDepartmentAsync(db, "1");
            var repo = new TeamRepository(db);
            var handler = new CreateTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateTeamCommandHandler>.Instance);

            var cmd = new CreateTeamCommand { DepartmentId = dept.Id, Name = "Platform", Code = "PLT" };
            var created = await handler.Handle(cmd, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.NotNull(created.CreatedBy);
            Assert.Equal("Platform", db.Teams.Single().Name);
        }

        [Fact]
        public async Task CreateTeam_DuplicateCodeInSameDepartment_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var dept = await SeedDepartmentAsync(db, "2");
            var repo = new TeamRepository(db);
            var handler = new CreateTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateTeamCommandHandler>.Instance);

            await handler.Handle(new CreateTeamCommand { DepartmentId = dept.Id, Name = "Core", Code = "COR" }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new CreateTeamCommand { DepartmentId = dept.Id, Name = "Core Two", Code = "COR" }, CancellationToken.None));
        }

        [Fact]
        public async Task CreateTeam_SameCodeInDifferentDepartment_Succeeds()
        {
            using var db = CreateDb();
            var deptA = await SeedDepartmentAsync(db, "3A");
            var deptB = await SeedDepartmentAsync(db, "3B");
            var repo = new TeamRepository(db);
            var handler = new CreateTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateTeamCommandHandler>.Instance);

            await handler.Handle(new CreateTeamCommand { DepartmentId = deptA.Id, Name = "Ops A", Code = "OPS" }, CancellationToken.None);
            var second = await handler.Handle(new CreateTeamCommand { DepartmentId = deptB.Id, Name = "Ops B", Code = "OPS" }, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, second.Id);
        }

        [Fact]
        public async Task CreateTeamCommandValidator_RejectsDuplicateCodeInDepartment()
        {
            using var db = CreateDb();
            var dept = await SeedDepartmentAsync(db, "4");
            var repo = new TeamRepository(db);
            await repo.AddAsync(new Team { Id = Guid.NewGuid(), DepartmentId = dept.Id, Name = "Existing", Code = "EXI", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var validator = new CreateTeamCommandValidator(repo, new DepartmentRepository(db));
            var result = await validator.ValidateAsync(new CreateTeamCommand { DepartmentId = dept.Id, Name = "New", Code = "EXI" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Code");
        }

        [Fact]
        public async Task CreateTeamCommandValidator_RejectsNonExistentDepartment()
        {
            using var db = CreateDb();
            var repo = new TeamRepository(db);
            var validator = new CreateTeamCommandValidator(repo, new DepartmentRepository(db));

            var result = await validator.ValidateAsync(new CreateTeamCommand { DepartmentId = Guid.NewGuid(), Name = "Ghost", Code = "GHO" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "DepartmentId");
        }

        [Fact]
        public async Task UpdateTeam_ChangesFields()
        {
            using var db = CreateDb();
            var dept = await SeedDepartmentAsync(db, "5");
            var repo = new TeamRepository(db);
            var createHandler = new CreateTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateTeamCommandHandler>.Instance);
            var updateHandler = new UpdateTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<UpdateTeamCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateTeamCommand { DepartmentId = dept.Id, Name = "Growth", Code = "GRW" }, CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateTeamCommand { Id = created.Id, DepartmentId = dept.Id, Name = "Growth & Marketing", Code = "GRW" }, CancellationToken.None);

            Assert.Equal("Growth & Marketing", updated.Name);
            Assert.NotNull(updated.UpdatedAtUtc);
            Assert.NotNull(updated.UpdatedBy);
        }

        [Fact]
        public async Task DeleteTeam_SoftDeletesAndHidesFromGetById()
        {
            using var db = CreateDb();
            var dept = await SeedDepartmentAsync(db, "6");
            var repo = new TeamRepository(db);
            var createHandler = new CreateTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateTeamCommandHandler>.Instance);
            var deleteHandler = new DeleteTeamCommandHandler(repo, new FakeCurrentUserService(), NullLogger<DeleteTeamCommandHandler>.Instance);

            var created = await createHandler.Handle(new CreateTeamCommand { DepartmentId = dept.Id, Name = "Legacy", Code = "LEG" }, CancellationToken.None);

            await deleteHandler.Handle(new DeleteTeamCommand { Id = created.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));
            var stillThere = await repo.GetByIdIncludingDeletedAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stillThere);
            Assert.True(stillThere!.IsDeleted);
        }

        [Fact]
        public async Task GetTeamsByDepartment_ReturnsOnlyTeamsInThatDepartment()
        {
            using var db = CreateDb();
            var deptA = await SeedDepartmentAsync(db, "7A");
            var deptB = await SeedDepartmentAsync(db, "7B");
            var repo = new TeamRepository(db);
            await repo.AddAsync(new Team { Id = Guid.NewGuid(), DepartmentId = deptA.Id, Name = "A1", Code = "A1", CreatedAtUtc = DateTime.UtcNow });
            await repo.AddAsync(new Team { Id = Guid.NewGuid(), DepartmentId = deptB.Id, Name = "B1", Code = "B1", CreatedAtUtc = DateTime.UtcNow });
            await repo.SaveChangesAsync();

            var result = await repo.GetByDepartmentAsync(deptA.Id, CancellationToken.None);

            Assert.Single(result);
        }
    }
}

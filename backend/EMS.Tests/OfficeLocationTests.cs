using EMS.Application.Features.OfficeLocations;
using EMS.Application.Features.OfficeLocations.Handlers;
using EMS.Application.Features.OfficeLocations.Validators;
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
    public class OfficeLocationTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_officelocation_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public string? IpAddress => null;
            public string? UserAgent => null;
        }

        private static CreateOfficeLocationCommand ValidCreateCommand(string code) => new()
        {
            Name = "Headquarters " + code,
            Code = code,
            City = "Seattle",
            Country = "USA",
            TimeZoneId = "America/Los_Angeles"
        };

        [Fact]
        public async Task CreateOfficeLocation_PersistsAndReturnsLocation()
        {
            using var db = CreateDb();
            var repo = new OfficeLocationRepository(db);
            var handler = new CreateOfficeLocationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateOfficeLocationCommandHandler>.Instance);

            var created = await handler.Handle(ValidCreateCommand("HQ1"), CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.NotNull(created.CreatedBy);
            Assert.Equal("Seattle", db.OfficeLocations.Single().City);
        }

        [Fact]
        public async Task CreateOfficeLocation_DuplicateCode_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new OfficeLocationRepository(db);
            var handler = new CreateOfficeLocationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateOfficeLocationCommandHandler>.Instance);

            await handler.Handle(ValidCreateCommand("HQ2"), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(ValidCreateCommand("HQ2"), CancellationToken.None));
        }

        [Fact]
        public async Task CreateOfficeLocationCommandValidator_RejectsDuplicateCode()
        {
            using var db = CreateDb();
            var repo = new OfficeLocationRepository(db);
            await repo.AddAsync(new EMS.Domain.Entities.OfficeLocation
            {
                Id = Guid.NewGuid(),
                Name = "Existing",
                Code = "HQ3",
                City = "Austin",
                Country = "USA",
                TimeZoneId = "America/Chicago",
                CreatedAtUtc = DateTime.UtcNow
            });
            await repo.SaveChangesAsync();

            var validator = new CreateOfficeLocationCommandValidator(repo);
            var result = await validator.ValidateAsync(ValidCreateCommand("HQ3"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Code");
        }

        [Fact]
        public async Task CreateOfficeLocationCommandValidator_RejectsMissingRequiredFields()
        {
            using var db = CreateDb();
            var repo = new OfficeLocationRepository(db);
            var validator = new CreateOfficeLocationCommandValidator(repo);

            var result = await validator.ValidateAsync(new CreateOfficeLocationCommand
            {
                Name = "",
                Code = "",
                City = "",
                Country = "",
                TimeZoneId = ""
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "City");
            Assert.Contains(result.Errors, e => e.PropertyName == "Country");
            Assert.Contains(result.Errors, e => e.PropertyName == "TimeZoneId");
        }

        [Fact]
        public async Task UpdateOfficeLocation_ChangesFields()
        {
            using var db = CreateDb();
            var repo = new OfficeLocationRepository(db);
            var createHandler = new CreateOfficeLocationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateOfficeLocationCommandHandler>.Instance);
            var updateHandler = new UpdateOfficeLocationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<UpdateOfficeLocationCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("HQ4"), CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateOfficeLocationCommand
            {
                Id = created.Id,
                Name = "Headquarters HQ4 Updated",
                Code = "HQ4",
                City = "Portland",
                Country = "USA",
                TimeZoneId = "America/Los_Angeles"
            }, CancellationToken.None);

            Assert.Equal("Portland", updated.City);
            Assert.NotNull(updated.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeleteOfficeLocation_SoftDeletesAndHidesFromGetById()
        {
            using var db = CreateDb();
            var repo = new OfficeLocationRepository(db);
            var createHandler = new CreateOfficeLocationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<CreateOfficeLocationCommandHandler>.Instance);
            var deleteHandler = new DeleteOfficeLocationCommandHandler(repo, new FakeCurrentUserService(), NullLogger<DeleteOfficeLocationCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("HQ5"), CancellationToken.None);

            await deleteHandler.Handle(new DeleteOfficeLocationCommand { Id = created.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));
            var stillThere = await repo.GetByIdIncludingDeletedAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stillThere);
            Assert.True(stillThere!.IsDeleted);
        }
    }
}

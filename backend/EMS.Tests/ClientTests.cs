using EMS.Application.Features.Clients;
using EMS.Application.Features.Clients.Handlers;
using EMS.Application.Features.Clients.Validators;
using EMS.Application.Interfaces;
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
    public class ClientTests
    {
        private static ApplicationDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ems_client_test_" + Guid.NewGuid())
                .Options;
            return new ApplicationDbContext(options);
        }

        private class FakeCurrentUserService : ICurrentUserService
        {
            public Guid? UserId => Guid.NewGuid();
            public string? IpAddress => null;
            public string? UserAgent => null;
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

        private static CreateClientCommand ValidCreateCommand(string name) => new()
        {
            ClientName = name,
            CompanyName = "Acme Corp",
            ContactPerson = "Jane Doe",
            MobileNumber = "+1-555-0100",
            Email = "jane@acme.example",
            AddressLine1 = "1 Market Street",
            City = "San Francisco",
            Country = "USA",
            PostalCode = "94105"
        };

        [Fact]
        public async Task CreateClient_PersistsAndReturnsClient()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var audit = new RecordingAuditLogger();
            var handler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), audit, NullLogger<CreateClientCommandHandler>.Instance);

            var created = await handler.Handle(ValidCreateCommand("Acme Retail"), CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.NotNull(created.CreatedBy);
            Assert.True(created.IsActive);
            Assert.False(created.IsArchived);
            Assert.Equal("San Francisco", db.Clients.Single().City);
            Assert.Contains(audit.Calls, c => c.EntityName == "Client" && c.Action == "Created");
        }

        [Fact]
        public async Task CreateClient_DuplicateName_ThrowsInvalidOperationException()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var handler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);

            await handler.Handle(ValidCreateCommand("Duplicate Client"), CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(ValidCreateCommand("Duplicate Client"), CancellationToken.None));
        }

        [Fact]
        public async Task CreateClientCommandValidator_RejectsDuplicateName()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            await repo.AddAsync(new EMS.Domain.Entities.Client
            {
                Id = Guid.NewGuid(),
                ClientName = "Existing Client",
                CompanyName = "Existing Co",
                ContactPerson = "John Smith",
                MobileNumber = "+1-555-0101",
                Email = "john@existing.example",
                AddressLine1 = "2 Market Street",
                City = "San Francisco",
                Country = "USA",
                PostalCode = "94105",
                CreatedAtUtc = DateTime.UtcNow
            });
            await repo.SaveChangesAsync();

            var validator = new CreateClientCommandValidator(repo);
            var result = await validator.ValidateAsync(ValidCreateCommand("Existing Client"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ClientName");
        }

        [Fact]
        public async Task CreateClientCommandValidator_RejectsMissingRequiredFieldsAndBadEmail()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var validator = new CreateClientCommandValidator(repo);

            var result = await validator.ValidateAsync(new CreateClientCommand
            {
                ClientName = "",
                CompanyName = "",
                ContactPerson = "",
                MobileNumber = "",
                Email = "not-an-email",
                AddressLine1 = "",
                City = "",
                Country = "",
                PostalCode = ""
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "ClientName");
            Assert.Contains(result.Errors, e => e.PropertyName == "CompanyName");
            Assert.Contains(result.Errors, e => e.PropertyName == "Email");
            Assert.Contains(result.Errors, e => e.PropertyName == "City");
        }

        [Fact]
        public async Task UpdateClient_ChangesFields()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var createHandler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);
            var updateHandler = new UpdateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<UpdateClientCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("Update Target"), CancellationToken.None);

            var updated = await updateHandler.Handle(new UpdateClientCommand
            {
                Id = created.Id,
                ClientName = "Update Target",
                CompanyName = "Acme Corp",
                ContactPerson = "Jane Doe",
                MobileNumber = "+1-555-0100",
                Email = "jane@acme.example",
                AddressLine1 = "1 Market Street",
                City = "Oakland",
                Country = "USA",
                PostalCode = "94612"
            }, CancellationToken.None);

            Assert.Equal("Oakland", updated.City);
            Assert.NotNull(updated.UpdatedAtUtc);
        }

        [Fact]
        public async Task DeleteClient_SoftDeletesAndHidesFromGetById()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var createHandler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);
            var deleteHandler = new DeleteClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeleteClientCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("Delete Target"), CancellationToken.None);

            await deleteHandler.Handle(new DeleteClientCommand { Id = created.Id }, CancellationToken.None);

            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));
            var stillThere = await repo.GetByIdIncludingDeletedAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stillThere);
            Assert.True(stillThere!.IsDeleted);
        }

        [Fact]
        public async Task ActivateDeactivate_ToggleIsActive()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var createHandler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);
            var deactivateHandler = new DeactivateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeactivateClientCommandHandler>.Instance);
            var activateHandler = new ActivateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ActivateClientCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("Toggle Target"), CancellationToken.None);
            Assert.True(created.IsActive);

            await deactivateHandler.Handle(new DeactivateClientCommand { Id = created.Id }, CancellationToken.None);
            Assert.False((await repo.GetByIdAsync(created.Id, CancellationToken.None))!.IsActive);

            await activateHandler.Handle(new ActivateClientCommand { Id = created.Id }, CancellationToken.None);
            Assert.True((await repo.GetByIdAsync(created.Id, CancellationToken.None))!.IsActive);
        }

        [Fact]
        public async Task Archive_SetsIsArchivedAndDeactivates_RestoreReversesBoth()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var createHandler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);
            var archiveHandler = new ArchiveClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<ArchiveClientCommandHandler>.Instance);
            var restoreHandler = new RestoreClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<RestoreClientCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("Archive Target"), CancellationToken.None);

            await archiveHandler.Handle(new ArchiveClientCommand { Id = created.Id }, CancellationToken.None);
            var archived = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.True(archived!.IsArchived);
            Assert.False(archived.IsActive);

            await restoreHandler.Handle(new RestoreClientCommand { Id = created.Id }, CancellationToken.None);
            var restored = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.False(restored!.IsArchived);
        }

        [Fact]
        public async Task Restore_UndeletesASoftDeletedClient()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var createHandler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);
            var deleteHandler = new DeleteClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeleteClientCommandHandler>.Instance);
            var restoreHandler = new RestoreClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<RestoreClientCommandHandler>.Instance);

            var created = await createHandler.Handle(ValidCreateCommand("Restore Target"), CancellationToken.None);
            await deleteHandler.Handle(new DeleteClientCommand { Id = created.Id }, CancellationToken.None);
            Assert.Null(await repo.GetByIdAsync(created.Id, CancellationToken.None));

            await restoreHandler.Handle(new RestoreClientCommand { Id = created.Id }, CancellationToken.None);

            var restored = await repo.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(restored);
            Assert.False(restored!.IsDeleted);
        }

        [Fact]
        public async Task GetClientsQuery_FiltersBySearchAndActiveStatus_AndPaginates()
        {
            using var db = CreateDb();
            var repo = new ClientRepository(db);
            var createHandler = new CreateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<CreateClientCommandHandler>.Instance);
            var deactivateHandler = new DeactivateClientCommandHandler(repo, new FakeCurrentUserService(), new RecordingAuditLogger(), NullLogger<DeactivateClientCommandHandler>.Instance);

            var alpha = await createHandler.Handle(ValidCreateCommand("Alpha Industries"), CancellationToken.None);
            await createHandler.Handle(ValidCreateCommand("Beta Logistics"), CancellationToken.None);
            await deactivateHandler.Handle(new DeactivateClientCommand { Id = alpha.Id }, CancellationToken.None);

            var queryHandler = new GetClientsQueryHandler(repo);

            var searchResult = await queryHandler.Handle(new GetClientsQuery { Search = "Alpha" }, CancellationToken.None);
            Assert.Single(searchResult.Data);
            Assert.Equal("Alpha Industries", searchResult.Data.Single().ClientName);

            var activeOnly = await queryHandler.Handle(new GetClientsQuery { IsActive = true }, CancellationToken.None);
            Assert.DoesNotContain(activeOnly.Data, c => c.ClientName == "Alpha Industries");

            var paged = await queryHandler.Handle(new GetClientsQuery { Page = 1, PageSize = 1 }, CancellationToken.None);
            Assert.Single(paged.Data);
            Assert.Equal(2, paged.TotalCount);
            Assert.Equal(2, paged.TotalPages);
        }
    }
}

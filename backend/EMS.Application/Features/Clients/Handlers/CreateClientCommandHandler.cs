using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Client>
    {
        private readonly IClientRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateClientCommandHandler> _logger;

        public CreateClientCommandHandler(IClientRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<CreateClientCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Client> Handle(CreateClientCommand request, CancellationToken cancellationToken)
        {
            if (await _repo.NameExistsAsync(request.ClientName, ct: cancellationToken))
                throw new InvalidOperationException("Client name already exists.");

            var client = new Client
            {
                Id = Guid.NewGuid(),
                ClientName = request.ClientName,
                CompanyName = request.CompanyName,
                ContactPerson = request.ContactPerson,
                MobileNumber = request.MobileNumber,
                AlternateMobile = request.AlternateMobile,
                Email = request.Email,
                GstNumber = request.GstNumber,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                State = request.State,
                Country = request.Country,
                PostalCode = request.PostalCode,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Notes = request.Notes,
                IsActive = true,
                IsArchived = false,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.UserId,
                IsDeleted = false
            };

            await _repo.AddAsync(client, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created client {ClientName} ({ClientId})", client.ClientName, client.Id);

            await _auditLogger.LogAsync("Client", client.Id, "Created", ct: cancellationToken);

            return client;
        }
    }
}

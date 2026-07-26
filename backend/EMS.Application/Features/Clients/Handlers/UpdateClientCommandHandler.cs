using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, Client>
    {
        private readonly IClientRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateClientCommandHandler> _logger;

        public UpdateClientCommandHandler(IClientRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<UpdateClientCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Client> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
        {
            var client = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Client {request.Id} not found.");

            if (await _repo.NameExistsAsync(request.ClientName, request.Id, cancellationToken))
                throw new InvalidOperationException("Client name already exists.");

            client.ClientName = request.ClientName;
            client.CompanyName = request.CompanyName;
            client.ContactPerson = request.ContactPerson;
            client.MobileNumber = request.MobileNumber;
            client.AlternateMobile = request.AlternateMobile;
            client.Email = request.Email;
            client.GstNumber = request.GstNumber;
            client.AddressLine1 = request.AddressLine1;
            client.AddressLine2 = request.AddressLine2;
            client.City = request.City;
            client.State = request.State;
            client.Country = request.Country;
            client.PostalCode = request.PostalCode;
            client.Latitude = request.Latitude;
            client.Longitude = request.Longitude;
            client.Notes = request.Notes;
            client.UpdatedAtUtc = DateTime.UtcNow;
            client.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(client, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated client {ClientId}", client.Id);

            await _auditLogger.LogAsync("Client", client.Id, "Updated", ct: cancellationToken);

            return client;
        }
    }
}

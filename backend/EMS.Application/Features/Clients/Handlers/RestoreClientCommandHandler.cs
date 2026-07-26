using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class RestoreClientCommandHandler : IRequestHandler<RestoreClientCommand>
    {
        private readonly IClientRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreClientCommandHandler> _logger;

        public RestoreClientCommandHandler(IClientRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<RestoreClientCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreClientCommand request, CancellationToken cancellationToken)
        {
            var client = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Client {request.Id} not found.");

            client.IsDeleted = false;
            client.DeletedAtUtc = null;
            client.DeletedBy = null;
            client.IsArchived = false;
            client.UpdatedAtUtc = DateTime.UtcNow;
            client.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(client, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored client {ClientId}", client.Id);

            await _auditLogger.LogAsync("Client", client.Id, "Restored", ct: cancellationToken);
        }
    }
}

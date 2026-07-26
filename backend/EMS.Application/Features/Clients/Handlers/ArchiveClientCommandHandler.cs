using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class ArchiveClientCommandHandler : IRequestHandler<ArchiveClientCommand>
    {
        private readonly IClientRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<ArchiveClientCommandHandler> _logger;

        public ArchiveClientCommandHandler(IClientRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<ArchiveClientCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(ArchiveClientCommand request, CancellationToken cancellationToken)
        {
            var client = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Client {request.Id} not found.");

            // Archived clients are retired from active workflows, so they are also deactivated.
            client.IsArchived = true;
            client.IsActive = false;
            client.UpdatedAtUtc = DateTime.UtcNow;
            client.UpdatedBy = _currentUser.UserId;

            await _repo.UpdateAsync(client, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Archived client {ClientId}", client.Id);

            await _auditLogger.LogAsync("Client", client.Id, "Archived", ct: cancellationToken);
        }
    }
}

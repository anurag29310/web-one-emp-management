using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Clients.Handlers
{
    public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand>
    {
        private readonly IClientRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteClientCommandHandler> _logger;

        public DeleteClientCommandHandler(IClientRepository repo, ICurrentUserService currentUser, IAuditLogger auditLogger, ILogger<DeleteClientCommandHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            var client = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Client {request.Id} not found.");

            client.DeletedAtUtc = DateTime.UtcNow;
            client.DeletedBy = _currentUser.UserId;

            await _repo.DeleteAsync(client, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) client {ClientId}", client.Id);

            await _auditLogger.LogAsync("Client", client.Id, "Deleted", ct: cancellationToken);
        }
    }
}

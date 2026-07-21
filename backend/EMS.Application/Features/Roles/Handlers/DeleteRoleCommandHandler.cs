using EMS.Application.Features.Roles.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Roles.Handlers
{
    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
    {
        private readonly IRoleRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(IRoleRepository repo, IAuditLogger auditLogger, ILogger<DeleteRoleCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Role {request.Id} not found.");

            if (await _repo.IsInUseAsync(role.Id, cancellationToken))
                throw new InvalidOperationException("Role is assigned to one or more active users and cannot be deleted.");

            await _repo.DeleteAsync(role, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) role {RoleId}", role.Id);

            await _auditLogger.LogAsync("Role", role.Id, "Deleted", oldValues: new { role.Name }, ct: cancellationToken);
        }
    }
}

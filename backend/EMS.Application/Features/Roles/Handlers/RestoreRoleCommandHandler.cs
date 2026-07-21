using EMS.Application.Features.Roles.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Roles.Handlers
{
    public class RestoreRoleCommandHandler : IRequestHandler<RestoreRoleCommand>
    {
        private readonly IRoleRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreRoleCommandHandler> _logger;

        public RestoreRoleCommandHandler(IRoleRepository repo, IAuditLogger auditLogger, ILogger<RestoreRoleCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _repo.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Role {request.Id} not found.");

            if (!role.IsDeleted)
                throw new InvalidOperationException("Role is not deleted and cannot be restored.");

            await _repo.RestoreAsync(role, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored role {RoleId}", role.Id);

            await _auditLogger.LogAsync("Role", role.Id, "Restored", ct: cancellationToken);
        }
    }
}

using EMS.Application.Features.Roles.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Roles.Handlers
{
    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Role>
    {
        private readonly IRoleRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(IRoleRepository repo, IAuditLogger auditLogger, ILogger<UpdateRoleCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Role> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _repo.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Role {request.Id} not found.");

            if (await _repo.NameExistsAsync(request.Name, request.Id, cancellationToken))
                throw new InvalidOperationException("Role name already exists.");

            var oldValues = new { role.Name, role.Description };

            role.Name = request.Name;
            role.Description = request.Description;
            role.UpdatedAtUtc = DateTime.UtcNow;

            await _repo.UpdateAsync(role, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated role {RoleId}", role.Id);

            await _auditLogger.LogAsync("Role", role.Id, "Updated",
                oldValues: oldValues, newValues: new { role.Name, role.Description }, ct: cancellationToken);

            return role;
        }
    }
}

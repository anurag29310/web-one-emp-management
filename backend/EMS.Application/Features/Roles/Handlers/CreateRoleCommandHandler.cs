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
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Role>
    {
        private readonly IRoleRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(IRoleRepository repo, IAuditLogger auditLogger, ILogger<CreateRoleCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Role> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            if (await _repo.NameExistsAsync(request.Name, ct: cancellationToken))
                throw new InvalidOperationException("Role name already exists.");

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repo.AddAsync(role, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created role {RoleName} ({RoleId})", role.Name, role.Id);

            await _auditLogger.LogAsync("Role", role.Id, "Created",
                newValues: new { role.Name, role.Description }, ct: cancellationToken);

            return role;
        }
    }
}

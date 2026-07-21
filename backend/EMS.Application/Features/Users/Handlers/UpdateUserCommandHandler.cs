using EMS.Application.Features.Users.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Users.Handlers
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, User>
    {
        private readonly IUserRepository _users;
        private readonly IRoleRepository _roles;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(IUserRepository users, IRoleRepository roles, IAuditLogger auditLogger, ILogger<UpdateUserCommandHandler> logger)
        {
            _users = users;
            _roles = roles;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<User> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"User {request.Id} not found.");

            if (await _users.UserNameExistsAsync(request.UserName, request.Id, cancellationToken))
                throw new InvalidOperationException("Username already exists.");
            if (await _users.EmailExistsAsync(request.Email, request.Id, cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            if (request.RoleId.HasValue && await _roles.GetByIdAsync(request.RoleId.Value, cancellationToken) == null)
                throw new InvalidOperationException($"Role {request.RoleId} not found.");

            var oldValues = new { user.UserName, user.Email, user.RoleId, user.EmployeeId };

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.RoleId = request.RoleId;
            user.EmployeeId = request.EmployeeId;
            user.UpdatedAtUtc = DateTime.UtcNow;

            await _users.UpdateAsync(user, cancellationToken);
            await _users.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated user {UserId}", user.Id);

            await _auditLogger.LogAsync("User", user.Id, "Updated",
                oldValues: oldValues,
                newValues: new { user.UserName, user.Email, user.RoleId, user.EmployeeId },
                ct: cancellationToken);

            return user;
        }
    }
}

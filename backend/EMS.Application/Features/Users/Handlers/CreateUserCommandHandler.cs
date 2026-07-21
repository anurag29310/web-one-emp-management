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
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, User>
    {
        private readonly IUserRepository _users;
        private readonly IRoleRepository _roles;
        private readonly IPasswordHashService _passwordHasher;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserRepository users,
            IRoleRepository roles,
            IPasswordHashService passwordHasher,
            IAuditLogger auditLogger,
            ILogger<CreateUserCommandHandler> logger)
        {
            _users = users;
            _roles = roles;
            _passwordHasher = passwordHasher;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (await _users.UserNameExistsAsync(request.UserName, ct: cancellationToken))
                throw new InvalidOperationException("Username already exists.");
            if (await _users.EmailExistsAsync(request.Email, ct: cancellationToken))
                throw new InvalidOperationException("Email already exists.");

            if (request.RoleId.HasValue && await _roles.GetByIdAsync(request.RoleId.Value, cancellationToken) == null)
                throw new InvalidOperationException($"Role {request.RoleId} not found.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.TemporaryPassword),
                RoleId = request.RoleId,
                EmployeeId = request.EmployeeId,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            await _users.AddAsync(user, cancellationToken);
            await _users.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created user {UserName} ({UserId})", user.UserName, user.Id);

            // Never include the password/hash in the audit trail.
            await _auditLogger.LogAsync("User", user.Id, "Created",
                newValues: new { user.UserName, user.Email, user.RoleId, user.IsActive }, ct: cancellationToken);

            return user;
        }
    }
}

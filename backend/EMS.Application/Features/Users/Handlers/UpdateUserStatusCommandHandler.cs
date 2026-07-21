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
    public class UpdateUserStatusCommandHandler : IRequestHandler<UpdateUserStatusCommand, User>
    {
        private readonly IUserRepository _users;
        private readonly IAuthRepository _auth;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<UpdateUserStatusCommandHandler> _logger;

        public UpdateUserStatusCommandHandler(IUserRepository users, IAuthRepository auth, IAuditLogger auditLogger, ILogger<UpdateUserStatusCommandHandler> logger)
        {
            _users = users;
            _auth = auth;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<User> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"User {request.Id} not found.");

            var wasActive = user.IsActive;
            user.IsActive = request.IsActive;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _users.UpdateAsync(user, cancellationToken);

            // Deactivating an account must not leave existing sessions valid.
            if (!request.IsActive)
                await _auth.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

            await _users.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Set user {UserId} active status to {IsActive}", user.Id, request.IsActive);

            await _auditLogger.LogAsync("User", user.Id, request.IsActive ? "Activated" : "Deactivated",
                oldValues: new { IsActive = wasActive }, newValues: new { user.IsActive }, ct: cancellationToken);

            return user;
        }
    }
}

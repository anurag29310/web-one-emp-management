using EMS.Application.Features.Users.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Users.Handlers
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IUserRepository _users;
        private readonly IAuthRepository _auth;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(IUserRepository users, IAuthRepository auth, IAuditLogger auditLogger, ILogger<DeleteUserCommandHandler> logger)
        {
            _users = users;
            _auth = auth;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"User {request.Id} not found.");

            await _users.DeleteAsync(user, cancellationToken);
            await _auth.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
            await _users.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted (soft) user {UserId}", user.Id);

            await _auditLogger.LogAsync("User", user.Id, "Deleted",
                oldValues: new { user.UserName, user.Email }, ct: cancellationToken);
        }
    }
}

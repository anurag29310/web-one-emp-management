using EMS.Application.Features.Users.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Users.Handlers
{
    public class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand>
    {
        private readonly IUserRepository _users;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreUserCommandHandler> _logger;

        public RestoreUserCommandHandler(IUserRepository users, IAuditLogger auditLogger, ILogger<RestoreUserCommandHandler> logger)
        {
            _users = users;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdIncludingDeletedAsync(request.Id, cancellationToken)
                ?? throw new InvalidOperationException($"User {request.Id} not found.");

            if (!user.IsDeleted)
                throw new InvalidOperationException("User is not deleted and cannot be restored.");

            await _users.RestoreAsync(user, cancellationToken);
            await _users.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Restored user {UserId}", user.Id);

            await _auditLogger.LogAsync("User", user.Id, "Restored", ct: cancellationToken);
        }
    }
}

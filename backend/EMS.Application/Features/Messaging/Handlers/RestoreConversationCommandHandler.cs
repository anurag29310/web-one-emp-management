using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class RestoreConversationCommandHandler : IRequestHandler<RestoreConversationCommand>
    {
        private readonly IMessagingRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<RestoreConversationCommandHandler> _logger;

        public RestoreConversationCommandHandler(IMessagingRepository repo, IAuditLogger auditLogger, ILogger<RestoreConversationCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(RestoreConversationCommand request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdIncludingDeletedAsync(request.Id, ct)
                ?? throw new InvalidOperationException($"Conversation {request.Id} not found.");

            if (!conversation.IsDeleted)
                throw new InvalidOperationException($"Conversation {conversation.Id} is not deleted.");

            conversation.DeletedAtUtc = null;
            conversation.DeletedBy = null;
            conversation.UpdatedAtUtc = DateTime.UtcNow;
            conversation.UpdatedBy = request.RequestingUserId;

            await _repo.RestoreConversationAsync(conversation, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Restored conversation {ConversationId}", conversation.Id);

            await _auditLogger.LogAsync("Conversation", conversation.Id, "Restored", ct: ct);
        }
    }
}

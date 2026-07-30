using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    /// <summary>Admin/HR-only moderation delete (gated by the CanManageMessaging policy at the controller) — not a participant-facing "delete for me" action.</summary>
    public class DeleteConversationCommandHandler : IRequestHandler<DeleteConversationCommand>
    {
        private readonly IMessagingRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<DeleteConversationCommandHandler> _logger;

        public DeleteConversationCommandHandler(IMessagingRepository repo, IAuditLogger auditLogger, ILogger<DeleteConversationCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(DeleteConversationCommand request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.Id, ct)
                ?? throw new InvalidOperationException($"Conversation {request.Id} not found.");

            conversation.DeletedAtUtc = DateTime.UtcNow;
            conversation.DeletedBy = request.RequestingUserId;
            await _repo.DeleteConversationAsync(conversation, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted conversation {ConversationId}", conversation.Id);

            await _auditLogger.LogAsync("Conversation", conversation.Id, "Deleted", ct: ct);
        }
    }
}

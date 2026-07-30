using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class LeaveConversationCommandHandler : IRequestHandler<LeaveConversationCommand>
    {
        private readonly IMessagingRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<LeaveConversationCommandHandler> _logger;

        public LeaveConversationCommandHandler(IMessagingRepository repo, IAuditLogger auditLogger, ILogger<LeaveConversationCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(LeaveConversationCommand request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            if (!conversation.IsGroup)
                throw new InvalidOperationException("Cannot leave a direct conversation.");

            var participant = await _repo.GetParticipantAsync(conversation.Id, request.RequestingUserId, ct);
            if (participant == null || participant.LeftAtUtc != null)
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            participant.LeftAtUtc = DateTime.UtcNow;
            await _repo.UpdateParticipantAsync(participant, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", request.RequestingUserId, conversation.Id);

            await _auditLogger.LogAsync("Conversation", conversation.Id, "ParticipantLeft", ct: ct);
        }
    }
}

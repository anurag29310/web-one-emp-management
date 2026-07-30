using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class AddParticipantsCommandHandler : IRequestHandler<AddParticipantsCommand>
    {
        private readonly IMessagingRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<AddParticipantsCommandHandler> _logger;

        public AddParticipantsCommandHandler(IMessagingRepository repo, IAuditLogger auditLogger, ILogger<AddParticipantsCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task Handle(AddParticipantsCommand request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            if (!await _repo.IsActiveParticipantAsync(conversation.Id, request.RequestingUserId, ct))
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            var now = DateTime.UtcNow;
            var anyAdded = false;

            foreach (var userId in request.UserIds.Distinct())
            {
                var existing = await _repo.GetParticipantAsync(conversation.Id, userId, ct);
                if (existing != null && existing.LeftAtUtc == null)
                    continue; // already an active participant

                if (existing != null)
                {
                    // Rejoining after having left — reset their read watermark to now so the
                    // history they missed while gone doesn't flood in as unread.
                    existing.LeftAtUtc = null;
                    existing.JoinedAtUtc = now;
                    existing.LastReadAtUtc = now;
                    await _repo.UpdateParticipantAsync(existing, ct);
                }
                else
                {
                    await _repo.AddParticipantAsync(new MessageParticipant
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        UserId = userId,
                        JoinedAtUtc = now,
                        LastReadAtUtc = now
                    }, ct);
                }

                anyAdded = true;
            }

            if (anyAdded)
            {
                // Adding a new participant to what was a 1:1 conversation always yields 3+ people.
                if (!conversation.IsGroup)
                    conversation.IsGroup = true;

                conversation.UpdatedAtUtc = now;
                conversation.UpdatedBy = request.RequestingUserId;
                await _repo.UpdateConversationAsync(conversation, ct);

                await _repo.SaveChangesAsync(ct);
                _logger.LogInformation("Added participants to conversation {ConversationId}", conversation.Id);
                await _auditLogger.LogAsync("Conversation", conversation.Id, "ParticipantsAdded", ct: ct);
            }
        }
    }
}

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
    public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, Guid>
    {
        private readonly IMessagingRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<CreateConversationCommandHandler> _logger;

        public CreateConversationCommandHandler(IMessagingRepository repo, IAuditLogger auditLogger, ILogger<CreateConversationCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateConversationCommand request, CancellationToken ct)
        {
            var otherUserIds = request.ParticipantUserIds.Distinct().Where(id => id != request.RequestingUserId).ToList();
            var now = DateTime.UtcNow;

            // A plain (untitled) 1:1 request reuses an existing direct conversation instead of
            // spawning a duplicate DM thread every time the two users start a new chat.
            if (otherUserIds.Count == 1 && string.IsNullOrWhiteSpace(request.Title))
            {
                var existing = await _repo.FindDirectConversationAsync(request.RequestingUserId, otherUserIds[0], ct);
                if (existing != null)
                {
                    var reusedMessage = new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = existing.Id,
                        SenderUserId = request.RequestingUserId,
                        Body = request.InitialMessageBody,
                        SentAtUtc = now
                    };
                    await _repo.AddMessageAsync(reusedMessage, ct);

                    existing.LastMessageAtUtc = now;
                    existing.UpdatedAtUtc = now;
                    existing.UpdatedBy = request.RequestingUserId;
                    await _repo.UpdateConversationAsync(existing, ct);

                    var senderParticipant = await _repo.GetParticipantAsync(existing.Id, request.RequestingUserId, ct);
                    if (senderParticipant != null)
                    {
                        senderParticipant.LastReadAtUtc = now;
                        await _repo.UpdateParticipantAsync(senderParticipant, ct);
                    }

                    await _repo.SaveChangesAsync(ct);
                    _logger.LogInformation("Reused existing direct conversation {ConversationId} for a new message instead of creating a duplicate", existing.Id);
                    await _auditLogger.LogAsync("Message", reusedMessage.Id, "Sent", ct: ct);

                    return existing.Id;
                }
            }

            var conversationId = Guid.NewGuid();
            var conversation = new Conversation
            {
                Id = conversationId,
                Title = request.Title,
                IsGroup = otherUserIds.Count > 1,
                CreatedByUserId = request.RequestingUserId,
                LastMessageAtUtc = now,
                IsDeleted = false,
                CreatedAtUtc = now,
                CreatedBy = request.RequestingUserId
            };
            await _repo.AddConversationAsync(conversation, ct);

            await _repo.AddParticipantAsync(new MessageParticipant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = request.RequestingUserId,
                JoinedAtUtc = now,
                LastReadAtUtc = now
            }, ct);

            foreach (var userId in otherUserIds)
            {
                await _repo.AddParticipantAsync(new MessageParticipant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    UserId = userId,
                    JoinedAtUtc = now,
                    LastReadAtUtc = null
                }, ct);
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderUserId = request.RequestingUserId,
                Body = request.InitialMessageBody,
                SentAtUtc = now
            };
            await _repo.AddMessageAsync(message, ct);

            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Created conversation {ConversationId} ({ParticipantCount} participants)", conversationId, otherUserIds.Count + 1);
            await _auditLogger.LogAsync("Conversation", conversationId, "Created", ct: ct);

            return conversationId;
        }
    }
}

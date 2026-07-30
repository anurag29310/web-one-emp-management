using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
    {
        private readonly IMessagingRepository _repo;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SendMessageCommandHandler> _logger;

        public SendMessageCommandHandler(IMessagingRepository repo, IAuditLogger auditLogger, ILogger<SendMessageCommandHandler> logger)
        {
            _repo = repo;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public async Task<Guid> Handle(SendMessageCommand request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            var participant = await _repo.GetParticipantAsync(conversation.Id, request.RequestingUserId, ct);
            if (participant == null || participant.LeftAtUtc != null)
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            var now = DateTime.UtcNow;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderUserId = request.RequestingUserId,
                Body = request.Body,
                SentAtUtc = now
            };
            await _repo.AddMessageAsync(message, ct);

            conversation.LastMessageAtUtc = now;
            conversation.UpdatedAtUtc = now;
            conversation.UpdatedBy = request.RequestingUserId;
            await _repo.UpdateConversationAsync(conversation, ct);

            // The sender has necessarily "read" their own message — advance their watermark so it
            // never shows up as unread in their own conversation list.
            participant.LastReadAtUtc = now;
            await _repo.UpdateParticipantAsync(participant, ct);

            await _repo.SaveChangesAsync(ct);
            _logger.LogInformation("Message {MessageId} sent in conversation {ConversationId}", message.Id, conversation.Id);
            await _auditLogger.LogAsync("Message", message.Id, "Sent", ct: ct);

            return message.Id;
        }
    }
}

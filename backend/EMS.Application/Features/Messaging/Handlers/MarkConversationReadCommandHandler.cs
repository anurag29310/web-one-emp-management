using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class MarkConversationReadCommandHandler : IRequestHandler<MarkConversationReadCommand>
    {
        private readonly IMessagingRepository _repo;

        public MarkConversationReadCommandHandler(IMessagingRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(MarkConversationReadCommand request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            var participant = await _repo.GetParticipantAsync(conversation.Id, request.RequestingUserId, ct);
            if (participant == null || participant.LeftAtUtc != null)
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            participant.LastReadAtUtc = DateTime.UtcNow;
            await _repo.UpdateParticipantAsync(participant, ct);
            await _repo.SaveChangesAsync(ct);
        }
    }
}

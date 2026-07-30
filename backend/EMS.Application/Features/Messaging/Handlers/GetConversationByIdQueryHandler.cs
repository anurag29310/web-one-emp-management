using EMS.Application.Features.Messaging.DTOs;
using EMS.Application.Features.Messaging.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class GetConversationByIdQueryHandler : IRequestHandler<GetConversationByIdQuery, ConversationDto?>
    {
        private readonly IMessagingRepository _repo;

        public GetConversationByIdQueryHandler(IMessagingRepository repo)
        {
            _repo = repo;
        }

        public async Task<ConversationDto?> Handle(GetConversationByIdQuery request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.Id, ct);
            if (conversation == null) return null;

            if (!request.IsPrivileged && !await _repo.IsActiveParticipantAsync(conversation.Id, request.RequestingUserId, ct))
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            var participants = await _repo.GetActiveParticipantsAsync(conversation.Id, ct);
            var names = await _repo.GetDisplayNamesAsync(participants.Select(p => p.UserId), ct);
            var participantDtos = participants.Select(p => new MessageParticipantDto
            {
                UserId = p.UserId,
                Name = names.TryGetValue(p.UserId, out var name) ? name : "Unknown",
                JoinedAtUtc = p.JoinedAtUtc,
                LeftAtUtc = p.LeftAtUtc
            }).ToList();

            var lastMessage = await _repo.GetLastMessageAsync(conversation.Id, ct);
            var self = participants.FirstOrDefault(p => p.UserId == request.RequestingUserId);
            var unread = self != null ? await _repo.CountUnreadAsync(conversation.Id, self.LastReadAtUtc, ct) : 0;

            return ConversationDto.FromEntity(conversation, participantDtos, lastMessage?.Body, unread);
        }
    }
}

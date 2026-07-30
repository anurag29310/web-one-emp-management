using EMS.Application.Common.DTOs;
using EMS.Application.Features.Messaging.DTOs;
using EMS.Application.Features.Messaging.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PagedResult<ConversationDto>>
    {
        private readonly IMessagingRepository _repo;

        public GetConversationsQueryHandler(IMessagingRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<ConversationDto>> Handle(GetConversationsQuery request, CancellationToken ct)
        {
            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var conversations = (await _repo.GetConversationsForUserAsync(request.RequestingUserId, page, pageSize, request.Search, ct)).ToList();
            var total = await _repo.CountConversationsForUserAsync(request.RequestingUserId, request.Search, ct);

            var dtos = new List<ConversationDto>();
            foreach (var conversation in conversations)
            {
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

                dtos.Add(ConversationDto.FromEntity(conversation, participantDtos, lastMessage?.Body, unread));
            }

            return PagedResult<ConversationDto>.Create(dtos, page, pageSize, total);
        }
    }
}

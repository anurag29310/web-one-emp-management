using EMS.Application.Common.DTOs;
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
    public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, PagedResult<MessageDto>>
    {
        private readonly IMessagingRepository _repo;

        public GetMessagesQueryHandler(IMessagingRepository repo)
        {
            _repo = repo;
        }

        public async Task<PagedResult<MessageDto>> Handle(GetMessagesQuery request, CancellationToken ct)
        {
            var conversation = await _repo.GetConversationByIdAsync(request.ConversationId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");

            if (!request.IsPrivileged && !await _repo.IsActiveParticipantAsync(conversation.Id, request.RequestingUserId, ct))
                throw new UnauthorizedAccessException("You are not a participant of this conversation.");

            var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var messages = (await _repo.GetMessagesAsync(conversation.Id, page, pageSize, ct)).ToList();
            var total = await _repo.CountMessagesAsync(conversation.Id, ct);

            var names = await _repo.GetDisplayNamesAsync(messages.Select(m => m.SenderUserId).Distinct(), ct);
            var dtos = messages.Select(m => MessageDto.FromEntity(m, names.TryGetValue(m.SenderUserId, out var name) ? name : null)).ToList();

            return PagedResult<MessageDto>.Create(dtos, page, pageSize, total);
        }
    }
}

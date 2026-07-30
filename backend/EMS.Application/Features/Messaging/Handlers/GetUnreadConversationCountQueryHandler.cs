using EMS.Application.Features.Messaging.Queries;
using EMS.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Messaging.Handlers
{
    public class GetUnreadConversationCountQueryHandler : IRequestHandler<GetUnreadConversationCountQuery, int>
    {
        private readonly IMessagingRepository _repo;

        public GetUnreadConversationCountQueryHandler(IMessagingRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> Handle(GetUnreadConversationCountQuery request, CancellationToken ct) =>
            await _repo.CountUnreadConversationsForUserAsync(request.RequestingUserId, ct);
    }
}

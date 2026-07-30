using EMS.Application.Common.DTOs;
using EMS.Application.Features.Messaging.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Queries
{
    public class GetConversationsQuery : IRequest<PagedResult<ConversationDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }

        public Guid RequestingUserId { get; set; }
    }
}

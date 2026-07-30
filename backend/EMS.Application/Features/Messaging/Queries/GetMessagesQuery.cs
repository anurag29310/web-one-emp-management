using EMS.Application.Common.DTOs;
using EMS.Application.Features.Messaging.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.Messaging.Queries
{
    public class GetMessagesQuery : IRequest<PagedResult<MessageDto>>
    {
        public Guid ConversationId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public Guid RequestingUserId { get; set; }
        public bool IsPrivileged { get; set; }
    }
}

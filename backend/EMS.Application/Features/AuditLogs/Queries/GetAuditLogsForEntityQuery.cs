using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.AuditLogs.Queries
{
    public class GetAuditLogsForEntityQuery : IRequest<PagedResult<AuditLogDto>>
    {
        public string EntityName { get; set; } = null!;
        public Guid EntityId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

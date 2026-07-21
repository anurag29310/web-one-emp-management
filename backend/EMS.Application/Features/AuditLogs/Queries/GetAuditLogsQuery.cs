using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.AuditLogs.Queries
{
    public class GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
    {
        public Guid? UserId { get; set; }
        public string? EntityName { get; set; }
        public Guid? EntityId { get; set; }
        public string? Action { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

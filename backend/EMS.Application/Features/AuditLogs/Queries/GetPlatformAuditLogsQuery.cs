using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using MediatR;
using System;

namespace EMS.Application.Features.AuditLogs.Queries
{
    /// <summary>Super Admin's cross-company audit log view. Unlike GetAuditLogsQuery, CompanyId is
    /// an explicit optional filter here (drill into one tenant) rather than always scoped to the
    /// caller's own company — a Super Admin has no "own company".</summary>
    public class GetPlatformAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
    {
        public Guid? CompanyId { get; set; }
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

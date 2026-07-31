using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.AuditLogs.Handlers
{
    public class GetPlatformAuditLogsQueryHandler : IRequestHandler<Queries.GetPlatformAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IAuditLogRepository _repo;

        public GetPlatformAuditLogsQueryHandler(IAuditLogRepository repo) => _repo = repo;

        public async Task<PagedResult<AuditLogDto>> Handle(Queries.GetPlatformAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetPagedAsync(
                request.CompanyId, request.UserId, request.EntityName, request.EntityId, request.Action,
                request.DateFrom, request.DateTo, page, pageSize, cancellationToken);
            var total = await _repo.CountAsync(
                request.CompanyId, request.UserId, request.EntityName, request.EntityId, request.Action,
                request.DateFrom, request.DateTo, cancellationToken);

            return PagedResult<AuditLogDto>.Create(items.Select(AuditLogDto.FromEntity), page, pageSize, total);
        }
    }
}

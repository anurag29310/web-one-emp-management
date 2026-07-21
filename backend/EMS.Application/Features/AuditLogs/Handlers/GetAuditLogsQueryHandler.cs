using EMS.Application.Common.DTOs;
using EMS.Application.Features.AuditLogs.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.AuditLogs.Handlers
{
    public class GetAuditLogsQueryHandler : IRequestHandler<Queries.GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IAuditLogRepository _repo;

        public GetAuditLogsQueryHandler(IAuditLogRepository repo) => _repo = repo;

        public async Task<PagedResult<AuditLogDto>> Handle(Queries.GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;

            var items = await _repo.GetPagedAsync(
                request.UserId, request.EntityName, request.EntityId, request.Action,
                request.DateFrom, request.DateTo, page, pageSize, cancellationToken);
            var total = await _repo.CountAsync(
                request.UserId, request.EntityName, request.EntityId, request.Action,
                request.DateFrom, request.DateTo, cancellationToken);

            return PagedResult<AuditLogDto>.Create(items.Select(AuditLogDto.FromEntity), page, pageSize, total);
        }
    }
}

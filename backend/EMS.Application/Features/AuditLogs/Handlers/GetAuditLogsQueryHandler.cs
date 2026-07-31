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
        private readonly ICurrentUserService _currentUser;

        public GetAuditLogsQueryHandler(IAuditLogRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<PagedResult<AuditLogDto>> Handle(Queries.GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 20;
            var page = request.Page > 0 ? request.Page : 1;
            var companyId = _currentUser.CompanyId;

            var items = await _repo.GetPagedAsync(
                companyId, request.UserId, request.EntityName, request.EntityId, request.Action,
                request.DateFrom, request.DateTo, page, pageSize, cancellationToken);
            var total = await _repo.CountAsync(
                companyId, request.UserId, request.EntityName, request.EntityId, request.Action,
                request.DateFrom, request.DateTo, cancellationToken);

            return PagedResult<AuditLogDto>.Create(items.Select(AuditLogDto.FromEntity), page, pageSize, total);
        }
    }
}

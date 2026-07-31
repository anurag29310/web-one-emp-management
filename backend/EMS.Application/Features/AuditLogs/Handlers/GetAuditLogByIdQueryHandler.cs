using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.AuditLogs.Handlers
{
    public class GetAuditLogByIdQueryHandler : IRequestHandler<Queries.GetAuditLogByIdQuery, AuditLog?>
    {
        private readonly IAuditLogRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetAuditLogByIdQueryHandler(IAuditLogRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task<AuditLog?> Handle(Queries.GetAuditLogByIdQuery request, CancellationToken cancellationToken)
        {
            var log = await _repo.GetByIdAsync(request.Id, cancellationToken);
            // Super Admin (CompanyId == null) sees every log; a tenant admin only ever sees their
            // own company's — treated as not-found on mismatch, matching this codebase's existing
            // ownership-check convention.
            if (log == null || (_currentUser.CompanyId.HasValue && log.CompanyId != _currentUser.CompanyId))
                return null;

            return log;
        }
    }
}

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

        public GetAuditLogByIdQueryHandler(IAuditLogRepository repo) => _repo = repo;

        public async Task<AuditLog?> Handle(Queries.GetAuditLogByIdQuery request, CancellationToken cancellationToken) =>
            await _repo.GetByIdAsync(request.Id, cancellationToken);
    }
}

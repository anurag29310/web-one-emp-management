using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log, CancellationToken ct = default);
        Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<AuditLog>> GetPagedAsync(
            Guid? userId, string? entityName, Guid? entityId, string? action,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountAsync(
            Guid? userId, string? entityName, Guid? entityId, string? action,
            DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
        Task<IEnumerable<AuditLog>> GetForEntityAsync(string entityName, Guid entityId, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountForEntityAsync(string entityName, Guid entityId, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

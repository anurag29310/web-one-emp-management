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

        /// <summary>companyId scopes results to a single tenant; pass null for the platform-wide
        /// (Super Admin) view with no tenant filter.</summary>
        Task<IEnumerable<AuditLog>> GetPagedAsync(
            Guid? companyId, Guid? userId, string? entityName, Guid? entityId, string? action,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountAsync(
            Guid? companyId, Guid? userId, string? entityName, Guid? entityId, string? action,
            DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
        Task<IEnumerable<AuditLog>> GetForEntityAsync(Guid? companyId, string entityName, Guid entityId, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountForEntityAsync(Guid? companyId, string entityName, Guid entityId, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

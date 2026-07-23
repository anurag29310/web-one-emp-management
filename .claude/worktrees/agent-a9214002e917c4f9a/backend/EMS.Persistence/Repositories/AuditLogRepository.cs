using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _db;

        public AuditLogRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
            await _db.AuditLogs.AddAsync(log, ct);

        public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<IEnumerable<AuditLog>> GetPagedAsync(
            Guid? userId, string? entityName, Guid? entityId, string? action,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default) =>
            await BuildFilterQuery(userId, entityName, entityId, action, dateFrom, dateTo)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(
            Guid? userId, string? entityName, Guid? entityId, string? action,
            DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) =>
            await BuildFilterQuery(userId, entityName, entityId, action, dateFrom, dateTo).CountAsync(ct);

        public async Task<IEnumerable<AuditLog>> GetForEntityAsync(string entityName, Guid entityId, int page, int pageSize, CancellationToken ct = default) =>
            await _db.AuditLogs.AsNoTracking()
                .Where(x => x.EntityName == entityName && x.EntityId == entityId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountForEntityAsync(string entityName, Guid entityId, CancellationToken ct = default) =>
            await _db.AuditLogs.AsNoTracking()
                .Where(x => x.EntityName == entityName && x.EntityId == entityId)
                .CountAsync(ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

        private IQueryable<AuditLog> BuildFilterQuery(
            Guid? userId, string? entityName, Guid? entityId, string? action, DateTime? dateFrom, DateTime? dateTo)
        {
            var q = _db.AuditLogs.AsNoTracking();

            if (userId.HasValue)
                q = q.Where(x => x.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(entityName))
                q = q.Where(x => x.EntityName == entityName);
            if (entityId.HasValue)
                q = q.Where(x => x.EntityId == entityId.Value);
            if (!string.IsNullOrWhiteSpace(action))
                q = q.Where(x => x.Action == action);
            if (dateFrom.HasValue)
                q = q.Where(x => x.CreatedAtUtc >= dateFrom.Value);
            if (dateTo.HasValue)
                q = q.Where(x => x.CreatedAtUtc <= dateTo.Value);

            return q;
        }
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class AssetRepository : IAssetRepository
    {
        private readonly ApplicationDbContext _db;

        public AssetRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Assets.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct);

        public async Task<Asset?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, ct);

        private IQueryable<Asset> BuildFilterQuery(AssetStatus? status, string? category, string? search)
        {
            var q = _db.Assets.AsNoTracking().Where(a => !a.IsDeleted);

            if (status.HasValue)
                q = q.Where(a => a.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(a => a.Category == category);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(a => a.AssetTag.Contains(search) || (a.Brand != null && a.Brand.Contains(search)) || (a.Model != null && a.Model.Contains(search)) || (a.SerialNumber != null && a.SerialNumber.Contains(search)));

            return q;
        }

        public async Task<IEnumerable<Asset>> GetAllAsync(int page, int pageSize, AssetStatus? status, string? category, string? search, CancellationToken ct = default) =>
            await BuildFilterQuery(status, category, search)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(AssetStatus? status, string? category, string? search, CancellationToken ct = default) =>
            await BuildFilterQuery(status, category, search).CountAsync(ct);

        public async Task<IEnumerable<Asset>> GetAllForExportAsync(AssetStatus? status, string? category, string? search, CancellationToken ct = default) =>
            await BuildFilterQuery(status, category, search)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync(ct);

        public async Task<bool> AssetTagExistsAsync(string assetTag, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Assets.AnyAsync(a => a.AssetTag == assetTag && (excludeId == null || a.Id != excludeId), ct);

        public async Task AddAsync(Asset asset, CancellationToken ct = default) =>
            await _db.Assets.AddAsync(asset, ct);

        public Task UpdateAsync(Asset asset, CancellationToken ct = default)
        {
            _db.Assets.Update(asset);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Asset asset, CancellationToken ct = default)
        {
            asset.IsDeleted = true;
            _db.Assets.Update(asset);
            return Task.CompletedTask;
        }

        public async Task<AssetAssignment?> GetAssignmentByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.AssetAssignments.Include(a => a.Asset).Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<IEnumerable<AssetAssignment>> GetAssignmentsByAssetAsync(Guid assetId, CancellationToken ct = default) =>
            await _db.AssetAssignments.AsNoTracking().Include(a => a.Employee)
                .Where(a => a.AssetId == assetId)
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync(ct);

        public async Task<IEnumerable<AssetAssignment>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
            await _db.AssetAssignments.AsNoTracking().Include(a => a.Asset)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync(ct);

        public async Task<AssetAssignment?> GetActiveAssignmentByAssetAsync(Guid assetId, CancellationToken ct = default) =>
            await _db.AssetAssignments.FirstOrDefaultAsync(a => a.AssetId == assetId && a.ReturnedDate == null, ct);

        public async Task<IEnumerable<AssetAssignment>> GetActiveAssignmentsByAssetIdsAsync(IEnumerable<Guid> assetIds, CancellationToken ct = default)
        {
            var idList = assetIds.ToList();
            return await _db.AssetAssignments.AsNoTracking().Include(a => a.Employee)
                .Where(a => idList.Contains(a.AssetId) && a.ReturnedDate == null)
                .ToListAsync(ct);
        }

        public async Task AddAssignmentAsync(AssetAssignment assignment, CancellationToken ct = default) =>
            await _db.AssetAssignments.AddAsync(assignment, ct);

        public Task UpdateAssignmentAsync(AssetAssignment assignment, CancellationToken ct = default)
        {
            _db.AssetAssignments.Update(assignment);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

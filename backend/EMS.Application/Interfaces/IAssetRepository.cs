using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IAssetRepository
    {
        // ─── Assets ────────────────────────────────────────────────────────────────
        Task<Asset?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Asset?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Asset>> GetAllAsync(int page, int pageSize, AssetStatus? status, string? category, string? search, CancellationToken ct = default);
        Task<int> CountAsync(AssetStatus? status, string? category, string? search, CancellationToken ct = default);
        Task<IEnumerable<Asset>> GetAllForExportAsync(AssetStatus? status, string? category, string? search, CancellationToken ct = default);
        Task<bool> AssetTagExistsAsync(string assetTag, Guid? excludeId = null, CancellationToken ct = default);
        Task AddAsync(Asset asset, CancellationToken ct = default);
        Task UpdateAsync(Asset asset, CancellationToken ct = default);
        Task DeleteAsync(Asset asset, CancellationToken ct = default);

        // ─── Assignments ───────────────────────────────────────────────────────────
        Task<AssetAssignment?> GetAssignmentByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<AssetAssignment>> GetAssignmentsByAssetAsync(Guid assetId, CancellationToken ct = default);
        Task<IEnumerable<AssetAssignment>> GetAssignmentsByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
        Task<AssetAssignment?> GetActiveAssignmentByAssetAsync(Guid assetId, CancellationToken ct = default);

        /// <summary>Batched form of <see cref="GetActiveAssignmentByAssetAsync"/> — one query for many assets, for export/report rendering.</summary>
        Task<IEnumerable<AssetAssignment>> GetActiveAssignmentsByAssetIdsAsync(IEnumerable<Guid> assetIds, CancellationToken ct = default);
        Task AddAssignmentAsync(AssetAssignment assignment, CancellationToken ct = default);
        Task UpdateAssignmentAsync(AssetAssignment assignment, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

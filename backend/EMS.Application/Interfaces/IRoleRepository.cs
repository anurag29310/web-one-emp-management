using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Role?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<IEnumerable<Role>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default);
        Task AddAsync(Role role, CancellationToken ct = default);
        Task UpdateAsync(Role role, CancellationToken ct = default);
        Task DeleteAsync(Role role, CancellationToken ct = default);
        Task RestoreAsync(Role role, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> IsInUseAsync(Guid roleId, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

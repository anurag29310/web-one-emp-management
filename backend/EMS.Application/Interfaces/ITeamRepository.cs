using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Team?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Team>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<Team>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
        Task AddAsync(Team team, CancellationToken ct = default);
        Task UpdateAsync(Team team, CancellationToken ct = default);
        Task DeleteAsync(Team team, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(Guid departmentId, string code, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

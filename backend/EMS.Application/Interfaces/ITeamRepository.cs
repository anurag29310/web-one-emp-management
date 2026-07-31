using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<Team?> GetByIdIncludingDeletedAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<Team>> GetAllAsync(Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<Team>> GetByDepartmentAsync(Guid departmentId, Guid companyId, CancellationToken ct = default);
        Task AddAsync(Team team, CancellationToken ct = default);
        Task UpdateAsync(Team team, CancellationToken ct = default);
        Task DeleteAsync(Team team, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(Guid departmentId, string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

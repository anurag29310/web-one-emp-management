using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<Department?> GetByIdIncludingDeletedAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<Department>> GetAllAsync(Guid companyId, CancellationToken ct = default);
        Task AddAsync(Department department, CancellationToken ct = default);
        Task UpdateAsync(Department department, CancellationToken ct = default);
        Task DeleteAsync(Department department, CancellationToken ct = default);
        Task RestoreAsync(Department department, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

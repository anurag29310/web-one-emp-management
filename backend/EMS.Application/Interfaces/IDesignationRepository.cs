using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IDesignationRepository
    {
        Task<Designation?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<Designation?> GetByIdIncludingDeletedAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<Designation>> GetAllAsync(Guid companyId, CancellationToken ct = default);
        Task AddAsync(Designation designation, CancellationToken ct = default);
        Task UpdateAsync(Designation designation, CancellationToken ct = default);
        Task DeleteAsync(Designation designation, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IOfficeLocationRepository
    {
        Task<OfficeLocation?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<OfficeLocation?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<OfficeLocation>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(OfficeLocation officeLocation, CancellationToken ct = default);
        Task UpdateAsync(OfficeLocation officeLocation, CancellationToken ct = default);
        Task DeleteAsync(OfficeLocation officeLocation, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

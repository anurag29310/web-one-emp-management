using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IOfficeLocationRepository
    {
        Task<OfficeLocation?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<OfficeLocation?> GetByIdIncludingDeletedAsync(Guid id, Guid companyId, CancellationToken ct = default);
        Task<IEnumerable<OfficeLocation>> GetAllAsync(Guid companyId, CancellationToken ct = default);
        Task AddAsync(OfficeLocation officeLocation, CancellationToken ct = default);
        Task UpdateAsync(OfficeLocation officeLocation, CancellationToken ct = default);
        Task DeleteAsync(OfficeLocation officeLocation, CancellationToken ct = default);
        Task<bool> CodeExistsAsync(string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

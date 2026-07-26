using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Client?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Client>> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default);
        Task<int> CountAsync(string? search, bool? isActive, CancellationToken ct = default);
        Task AddAsync(Client client, CancellationToken ct = default);
        Task UpdateAsync(Client client, CancellationToken ct = default);
        Task DeleteAsync(Client client, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string clientName, Guid? excludeId = null, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<User>> GetAllAsync(bool includeDeleted = false, Guid? roleId = null, bool? isActive = null, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
        Task UpdateAsync(User user, CancellationToken ct = default);
        Task DeleteAsync(User user, CancellationToken ct = default);
        Task RestoreAsync(User user, CancellationToken ct = default);
        Task<bool> UserNameExistsAsync(string userName, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
        Task<bool> AnyExistAsync(CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

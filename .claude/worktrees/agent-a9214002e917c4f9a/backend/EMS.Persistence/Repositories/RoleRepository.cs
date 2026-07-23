using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _db;

        public RoleRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

        public async Task<Role?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
            await _db.Roles.FirstOrDefaultAsync(r => r.Name == name && !r.IsDeleted, ct);

        public async Task<IEnumerable<Role>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
        {
            var query = _db.Roles.AsNoTracking().AsQueryable();
            if (!includeDeleted)
                query = query.Where(r => !r.IsDeleted);

            return await query.OrderBy(r => r.Name).ToListAsync(ct);
        }

        public async Task AddAsync(Role role, CancellationToken ct = default) =>
            await _db.Roles.AddAsync(role, ct);

        public Task UpdateAsync(Role role, CancellationToken ct = default)
        {
            _db.Roles.Update(role);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Role role, CancellationToken ct = default)
        {
            role.IsDeleted = true;
            _db.Roles.Update(role);
            return Task.CompletedTask;
        }

        public Task RestoreAsync(Role role, CancellationToken ct = default)
        {
            role.IsDeleted = false;
            _db.Roles.Update(role);
            return Task.CompletedTask;
        }

        public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Roles.AnyAsync(r => r.Name == name && !r.IsDeleted && (excludeId == null || r.Id != excludeId), ct);

        public async Task<bool> IsInUseAsync(Guid roleId, CancellationToken ct = default) =>
            await _db.Users.AnyAsync(u => u.RoleId == roleId && !u.IsDeleted, ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

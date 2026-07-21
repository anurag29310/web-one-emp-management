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
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;

        public UserRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct);

        public async Task<User?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<IEnumerable<User>> GetAllAsync(bool includeDeleted = false, Guid? roleId = null, bool? isActive = null, CancellationToken ct = default)
        {
            var query = _db.Users.Include(u => u.Role).AsNoTracking().AsQueryable();

            if (!includeDeleted)
                query = query.Where(u => !u.IsDeleted);
            if (roleId.HasValue)
                query = query.Where(u => u.RoleId == roleId.Value);
            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query.OrderBy(u => u.UserName).ToListAsync(ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default) =>
            await _db.Users.AddAsync(user, ct);

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Update(user);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(User user, CancellationToken ct = default)
        {
            user.IsDeleted = true;
            _db.Users.Update(user);
            return Task.CompletedTask;
        }

        public Task RestoreAsync(User user, CancellationToken ct = default)
        {
            user.IsDeleted = false;
            _db.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task<bool> UserNameExistsAsync(string userName, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Users.AnyAsync(u => u.UserName == userName && !u.IsDeleted && (excludeId == null || u.Id != excludeId), ct);

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Users.AnyAsync(u => u.Email == email && !u.IsDeleted && (excludeId == null || u.Id != excludeId), ct);

        public async Task<bool> AnyExistAsync(CancellationToken ct = default) =>
            await _db.Users.AnyAsync(ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

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
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _db;

        public ClientRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Clients.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        public async Task<Client?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);

        private IQueryable<Client> BuildFilterQuery(string? search, bool? isActive)
        {
            var q = _db.Clients.AsNoTracking().Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(c => c.ClientName.Contains(search) || c.CompanyName.Contains(search)
                    || c.ContactPerson.Contains(search) || c.Email.Contains(search));
            }

            if (isActive.HasValue)
                q = q.Where(c => c.IsActive == isActive.Value);

            return q;
        }

        public async Task<IEnumerable<Client>> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default) =>
            await BuildFilterQuery(search, isActive)
                .OrderBy(c => c.ClientName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(string? search, bool? isActive, CancellationToken ct = default) =>
            await BuildFilterQuery(search, isActive).CountAsync(ct);

        public async Task AddAsync(Client client, CancellationToken ct = default) =>
            await _db.Clients.AddAsync(client, ct);

        public Task UpdateAsync(Client client, CancellationToken ct = default)
        {
            _db.Clients.Update(client);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Client client, CancellationToken ct = default)
        {
            client.IsDeleted = true;
            _db.Clients.Update(client);
            return Task.CompletedTask;
        }

        public async Task<bool> NameExistsAsync(string clientName, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Clients.AnyAsync(c => c.ClientName == clientName && !c.IsDeleted && (excludeId == null || c.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

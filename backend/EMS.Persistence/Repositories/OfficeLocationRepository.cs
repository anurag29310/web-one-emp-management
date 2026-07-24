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
    public class OfficeLocationRepository : IOfficeLocationRepository
    {
        private readonly ApplicationDbContext _db;

        public OfficeLocationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<OfficeLocation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.OfficeLocations.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, ct);

        public async Task<OfficeLocation?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.OfficeLocations.FirstOrDefaultAsync(o => o.Id == id, ct);

        public async Task<IEnumerable<OfficeLocation>> GetAllAsync(CancellationToken ct = default) =>
            await _db.OfficeLocations.AsNoTracking().Where(o => !o.IsDeleted).ToListAsync(ct);

        public async Task AddAsync(OfficeLocation officeLocation, CancellationToken ct = default) =>
            await _db.OfficeLocations.AddAsync(officeLocation, ct);

        public Task UpdateAsync(OfficeLocation officeLocation, CancellationToken ct = default)
        {
            _db.OfficeLocations.Update(officeLocation);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(OfficeLocation officeLocation, CancellationToken ct = default)
        {
            officeLocation.IsDeleted = true;
            _db.OfficeLocations.Update(officeLocation);
            return Task.CompletedTask;
        }

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.OfficeLocations.AnyAsync(o => o.Code == code && !o.IsDeleted && (excludeId == null || o.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

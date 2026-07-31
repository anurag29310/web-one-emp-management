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
    public class DesignationRepository : IDesignationRepository
    {
        private readonly ApplicationDbContext _db;

        public DesignationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Designation?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
            await _db.Designations.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId && !d.IsDeleted, ct);

        public async Task<Designation?> GetByIdIncludingDeletedAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
            await _db.Designations.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, ct);

        public async Task<IEnumerable<Designation>> GetAllAsync(Guid companyId, CancellationToken ct = default) =>
            await _db.Designations.AsNoTracking().Where(d => d.CompanyId == companyId && !d.IsDeleted).ToListAsync(ct);

        public async Task AddAsync(Designation designation, CancellationToken ct = default) =>
            await _db.Designations.AddAsync(designation, ct);

        public Task UpdateAsync(Designation designation, CancellationToken ct = default)
        {
            _db.Designations.Update(designation);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Designation designation, CancellationToken ct = default)
        {
            designation.IsDeleted = true;
            _db.Designations.Update(designation);
            return Task.CompletedTask;
        }

        public async Task<bool> NameExistsAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Designations.AnyAsync(d => d.Name == name && d.CompanyId == companyId && !d.IsDeleted && (excludeId == null || d.Id != excludeId), ct);

        public async Task<bool> CodeExistsAsync(string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Designations.AnyAsync(d => d.Code == code && d.CompanyId == companyId && !d.IsDeleted && (excludeId == null || d.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

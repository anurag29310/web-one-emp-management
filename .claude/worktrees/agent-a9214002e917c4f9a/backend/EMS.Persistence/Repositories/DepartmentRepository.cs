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
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly ApplicationDbContext _db;

        public DepartmentRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, ct);

        public async Task<Department?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

        public async Task<IEnumerable<Department>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Departments.AsNoTracking().Where(d => !d.IsDeleted).ToListAsync(ct);

        public async Task AddAsync(Department department, CancellationToken ct = default) =>
            await _db.Departments.AddAsync(department, ct);

        public Task UpdateAsync(Department department, CancellationToken ct = default)
        {
            _db.Departments.Update(department);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Department department, CancellationToken ct = default)
        {
            department.IsDeleted = true;
            _db.Departments.Update(department);
            return Task.CompletedTask;
        }

        public Task RestoreAsync(Department department, CancellationToken ct = default)
        {
            department.IsDeleted = false;
            _db.Departments.Update(department);
            return Task.CompletedTask;
        }

        public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Departments.AnyAsync(d => d.Name == name && !d.IsDeleted && (excludeId == null || d.Id != excludeId), ct);

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Departments.AnyAsync(d => d.Code == code && !d.IsDeleted && (excludeId == null || d.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

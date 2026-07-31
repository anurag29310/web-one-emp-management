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

        public async Task<Department?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
            await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId && !d.IsDeleted, ct);

        public async Task<Department?> GetByIdIncludingDeletedAsync(Guid id, Guid companyId, CancellationToken ct = default) =>
            await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, ct);

        public async Task<IEnumerable<Department>> GetAllAsync(Guid companyId, CancellationToken ct = default) =>
            await _db.Departments.AsNoTracking().Where(d => d.CompanyId == companyId && !d.IsDeleted).ToListAsync(ct);

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

        public async Task<bool> NameExistsAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Departments.AnyAsync(d => d.Name == name && d.CompanyId == companyId && !d.IsDeleted && (excludeId == null || d.Id != excludeId), ct);

        public async Task<bool> CodeExistsAsync(string code, Guid companyId, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Departments.AnyAsync(d => d.Code == code && d.CompanyId == companyId && !d.IsDeleted && (excludeId == null || d.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public async Task AddAsync(Department department)
        {
            await _db.Departments.AddAsync(department);
        }

        public async Task DeleteAsync(Department department)
        {
            _db.Departments.Update(department);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            return await _db.Departments.AsNoTracking().Where(d => !d.IsDeleted).ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(Guid id)
        {
            return await _db.Departments.FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        }

        public async Task UpdateAsync(Department department)
        {
            _db.Departments.Update(department);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}

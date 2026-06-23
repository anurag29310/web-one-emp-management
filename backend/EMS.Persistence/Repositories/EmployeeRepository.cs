using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _db;

        public EmployeeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Employee employee)
        {
            await _db.Employees.AddAsync(employee);
        }

        public async Task DeleteAsync(Employee employee)
        {
            employee.IsActive = false;
            _db.Employees.Update(employee);
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Employee>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, string? sortDir, Guid? departmentId, string? status)
        {
            var q = _db.Employees.AsNoTracking().Where(e => e.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(e => e.FirstName.Contains(search) || e.LastName.Contains(search) || e.EmployeeCode.Contains(search) || (e.Email != null && e.Email.Contains(search)));
            }
            if (departmentId.HasValue)
                q = q.Where(e => e.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(e => e.EmploymentStatus == status);

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
                q = sortBy switch
                {
                    "firstName" => desc ? q.OrderByDescending(e => e.FirstName) : q.OrderBy(e => e.FirstName),
                    "lastName" => desc ? q.OrderByDescending(e => e.LastName) : q.OrderBy(e => e.LastName),
                    "employeeCode" => desc ? q.OrderByDescending(e => e.EmployeeCode) : q.OrderBy(e => e.EmployeeCode),
                    _ => q.OrderBy(e => e.FirstName)
                };
            }
            else
            {
                q = q.OrderBy(e => e.FirstName);
            }

            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(Guid id)
        {
            return await _db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id && e.IsActive);
        }

        public async Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, int page, int pageSize)
        {
            return await _db.Employees.AsNoTracking().Where(e => e.DepartmentId == departmentId && e.IsActive).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetReportingEmployeesAsync(Guid managerId, int page, int pageSize)
        {
            return await _db.Employees.AsNoTracking().Where(e => e.ManagerId == managerId && e.IsActive).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<bool> EmployeeCodeExistsAsync(string code, Guid? excludeId = null)
        {
            return await _db.Employees.AnyAsync(e => e.EmployeeCode == code && (excludeId == null || e.Id != excludeId));
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null)
        {
            return await _db.Employees.AnyAsync(e => e.Email == email && (excludeId == null || e.Id != excludeId));
        }

        public async Task UpdateAsync(Employee employee)
        {
            _db.Employees.Update(employee);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}

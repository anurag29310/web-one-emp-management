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
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _db;

        public EmployeeRepository(ApplicationDbContext db) => _db = db;

        public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive, ct);

        public async Task<Employee?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id, ct);

        public async Task<IEnumerable<Employee>> GetAllAsync(
            int page, int pageSize, string? search, string? sortBy, string? sortDir,
            Guid? departmentId, string? status, CancellationToken ct = default)
        {
            var q = BuildFilterQuery(search, departmentId, status);
            q = ApplySort(q, sortBy, sortDir);
            return await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        }

        public async Task<int> CountAsync(string? search, Guid? departmentId, string? status, CancellationToken ct = default) =>
            await BuildFilterQuery(search, departmentId, status).CountAsync(ct);

        public async Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default) =>
            await _db.Employees.AsNoTracking()
                .Where(e => e.DepartmentId == departmentId && e.IsActive)
                .OrderBy(e => e.FirstName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<IEnumerable<Employee>> GetDirectReportsAsync(Guid managerId, int page, int pageSize, CancellationToken ct = default) =>
            await _db.Employees.AsNoTracking()
                .Where(e => e.ManagerId == managerId && e.IsActive)
                .OrderBy(e => e.FirstName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<IEnumerable<Guid>> GetDirectReportIdsAsync(Guid managerId, CancellationToken ct = default) =>
            await _db.Employees.AsNoTracking()
                .Where(e => e.ManagerId == managerId && e.IsActive)
                .Select(e => e.Id)
                .ToListAsync(ct);

        public async Task<bool> IsDirectReportAsync(Guid managerId, Guid employeeId, CancellationToken ct = default) =>
            await _db.Employees.AsNoTracking()
                .AnyAsync(e => e.Id == employeeId && e.ManagerId == managerId && e.IsActive, ct);

        public async Task<IEnumerable<Employee>> GetManagerChainAsync(Guid employeeId, CancellationToken ct = default)
        {
            var chain = new List<Employee>();
            var current = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

            var visited = new HashSet<Guid>();

            while (current?.ManagerId != null && !visited.Contains(current.Id))
            {
                visited.Add(current.Id);
                var manager = await _db.Employees.AsNoTracking()
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == current.ManagerId, ct);

                if (manager == null) break;
                chain.Add(manager);
                current = manager;
            }

            return chain;
        }

        public async Task AddAsync(Employee employee, CancellationToken ct = default) =>
            await _db.Employees.AddAsync(employee, ct);

        public Task UpdateAsync(Employee employee, CancellationToken ct = default)
        {
            _db.Employees.Update(employee);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Employee employee, CancellationToken ct = default)
        {
            employee.IsActive = false;
            _db.Employees.Update(employee);
            return Task.CompletedTask;
        }

        public Task RestoreAsync(Employee employee, CancellationToken ct = default)
        {
            employee.IsActive = true;
            employee.ExitDate = null;
            employee.EmploymentStatus = "Active";
            _db.Employees.Update(employee);
            return Task.CompletedTask;
        }

        public async Task<bool> EmployeeCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Employees.AnyAsync(e => e.EmployeeCode == code && (excludeId == null || e.Id != excludeId), ct);

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Employees.AnyAsync(e => e.Email == email && (excludeId == null || e.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private IQueryable<Employee> BuildFilterQuery(string? search, Guid? departmentId, string? status)
        {
            var q = _db.Employees.AsNoTracking().Where(e => e.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => e.FirstName.Contains(search) || e.LastName.Contains(search)
                    || e.EmployeeCode.Contains(search) || (e.Email != null && e.Email.Contains(search)));

            if (departmentId.HasValue)
                q = q.Where(e => e.DepartmentId == departmentId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(e => e.EmploymentStatus == status);

            return q;
        }

        private static IQueryable<Employee> ApplySort(IQueryable<Employee> q, string? sortBy, string? sortDir)
        {
            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            return sortBy switch
            {
                "firstName" => desc ? q.OrderByDescending(e => e.FirstName) : q.OrderBy(e => e.FirstName),
                "lastName" => desc ? q.OrderByDescending(e => e.LastName) : q.OrderBy(e => e.LastName),
                "employeeCode" => desc ? q.OrderByDescending(e => e.EmployeeCode) : q.OrderBy(e => e.EmployeeCode),
                "joinDate" => desc ? q.OrderByDescending(e => e.JoinDate) : q.OrderBy(e => e.JoinDate),
                _ => q.OrderBy(e => e.FirstName)
            };
        }
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ApplicationDbContext _db;

        public CompanyRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Companies.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        public async Task<Company?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);

        private IQueryable<Company> BuildFilterQuery(CompanyStatus? status, string? search)
        {
            var q = _db.Companies.AsNoTracking().Where(c => !c.IsDeleted);

            if (status.HasValue)
                q = q.Where(c => c.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.Name.Contains(search));

            return q;
        }

        public async Task<IEnumerable<Company>> GetAllAsync(int page, int pageSize, CompanyStatus? status, string? search, CancellationToken ct = default) =>
            await BuildFilterQuery(status, search)
                .OrderByDescending(c => c.RegisteredAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(CompanyStatus? status, string? search, CancellationToken ct = default) =>
            await BuildFilterQuery(status, search).CountAsync(ct);

        public async Task AddAsync(Company company, CancellationToken ct = default) =>
            await _db.Companies.AddAsync(company, ct);

        public Task UpdateAsync(Company company, CancellationToken ct = default)
        {
            _db.Companies.Update(company);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Company company, CancellationToken ct = default)
        {
            company.IsDeleted = true;
            _db.Companies.Update(company);
            return Task.CompletedTask;
        }

        public Task RestoreAsync(Company company, CancellationToken ct = default)
        {
            company.IsDeleted = false;
            _db.Companies.Update(company);
            return Task.CompletedTask;
        }

        public async Task<int> GetEmployeeCountAsync(Guid companyId, CancellationToken ct = default) =>
            await _db.Employees.CountAsync(e => e.CompanyId == companyId && !e.IsDeleted, ct);

        public async Task<IEnumerable<User>> GetAdminsAsync(Guid companyId, CancellationToken ct = default) =>
            await _db.Users.AsNoTracking().Include(u => u.Role)
                .Where(u => u.CompanyId == companyId && !u.IsDeleted && u.Role != null && u.Role.Name == "Admin")
                .ToListAsync(ct);

        public async Task<int> GetTotalEmployeeCountAcrossAllCompaniesAsync(CancellationToken ct = default) =>
            await _db.Employees.CountAsync(e => !e.IsDeleted, ct);

        public async Task<IReadOnlyDictionary<CompanyStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
        {
            var counts = await _db.Companies.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return counts.ToDictionary(x => x.Status, x => x.Count);
        }

        public async Task<IEnumerable<Company>> GetRecentRegistrationsAsync(int count, CancellationToken ct = default) =>
            await _db.Companies.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.RegisteredAtUtc)
                .Take(count)
                .ToListAsync(ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

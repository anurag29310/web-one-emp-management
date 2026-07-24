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
    public class TeamRepository : ITeamRepository
    {
        private readonly ApplicationDbContext _db;

        public TeamRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Teams.Include(t => t.Department).FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

        public async Task<Team?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await _db.Teams.Include(t => t.Department).FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task<IEnumerable<Team>> GetAllAsync(CancellationToken ct = default) =>
            await _db.Teams.AsNoTracking().Include(t => t.Department).Where(t => !t.IsDeleted).ToListAsync(ct);

        public async Task<IEnumerable<Team>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default) =>
            await _db.Teams.AsNoTracking().Include(t => t.Department).Where(t => t.DepartmentId == departmentId && !t.IsDeleted).ToListAsync(ct);

        public async Task AddAsync(Team team, CancellationToken ct = default) =>
            await _db.Teams.AddAsync(team, ct);

        public Task UpdateAsync(Team team, CancellationToken ct = default)
        {
            _db.Teams.Update(team);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Team team, CancellationToken ct = default)
        {
            team.IsDeleted = true;
            _db.Teams.Update(team);
            return Task.CompletedTask;
        }

        public async Task<bool> CodeExistsAsync(Guid departmentId, string code, Guid? excludeId = null, CancellationToken ct = default) =>
            await _db.Teams.AnyAsync(t => t.DepartmentId == departmentId && t.Code == code && !t.IsDeleted && (excludeId == null || t.Id != excludeId), ct);

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

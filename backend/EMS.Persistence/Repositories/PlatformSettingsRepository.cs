using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class PlatformSettingsRepository : IPlatformSettingsRepository
    {
        private readonly ApplicationDbContext _db;

        public PlatformSettingsRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PlatformSettings> GetAsync(CancellationToken ct = default) =>
            await _db.PlatformSettings.FirstAsync(x => x.Id == PlatformSettings.SingletonId, ct);

        public Task UpdateAsync(PlatformSettings settings, CancellationToken ct = default)
        {
            _db.PlatformSettings.Update(settings);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

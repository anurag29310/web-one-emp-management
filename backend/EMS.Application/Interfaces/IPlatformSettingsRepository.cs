using EMS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    /// <summary>Singleton settings row — no Create/Delete, only Get/Update against the fixed seeded Id.</summary>
    public interface IPlatformSettingsRepository
    {
        Task<PlatformSettings> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(PlatformSettings settings, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

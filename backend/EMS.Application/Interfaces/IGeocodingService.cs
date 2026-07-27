using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IGeocodingService
    {
        /// <summary>Best-effort reverse geocode. Returns null on provider failure rather than throwing — a punch must never be blocked by a geocoding outage.</summary>
        Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude, CancellationToken ct = default);
    }
}

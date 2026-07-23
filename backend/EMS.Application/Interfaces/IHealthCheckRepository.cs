using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IHealthCheckRepository
    {
        Task<bool> CanConnectToDatabaseAsync(CancellationToken ct = default);
    }
}

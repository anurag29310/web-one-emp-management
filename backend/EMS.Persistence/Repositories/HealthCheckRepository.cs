using EMS.Application.Interfaces;
using EMS.Persistence.Context;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class HealthCheckRepository : IHealthCheckRepository
    {
        private readonly ApplicationDbContext _db;

        public HealthCheckRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<bool> CanConnectToDatabaseAsync(CancellationToken ct = default) =>
            _db.Database.CanConnectAsync(ct);
    }
}

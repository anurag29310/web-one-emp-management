using EMS.Application.DTOs;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Health.Handlers
{
    public class GetReadinessQueryHandler : IRequestHandler<Queries.GetReadinessQuery, ReadinessStatusDto>
    {
        private readonly IHealthCheckRepository _repo;
        private readonly ILogger<GetReadinessQueryHandler> _logger;

        public GetReadinessQueryHandler(IHealthCheckRepository repo, ILogger<GetReadinessQueryHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ReadinessStatusDto> Handle(Queries.GetReadinessQuery request, CancellationToken cancellationToken)
        {
            var connected = await _repo.CanConnectToDatabaseAsync(cancellationToken);

            if (!connected)
            {
                _logger.LogError("Readiness check failed: database is not reachable.");
            }

            return new ReadinessStatusDto
            {
                Status = connected ? "Healthy" : "Unhealthy",
                DatabaseConnected = connected,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }
}

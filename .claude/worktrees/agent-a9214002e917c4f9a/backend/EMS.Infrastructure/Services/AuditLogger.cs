using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly IAuditLogRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public AuditLogger(IAuditLogRepository repo, ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public async Task LogAsync(
            string entityName,
            Guid? entityId,
            string action,
            object? oldValues = null,
            object? newValues = null,
            CancellationToken ct = default)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.UserId,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues),
                IpAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _repo.AddAsync(log, ct);
            await _repo.SaveChangesAsync(ct);
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    /// <summary>Records a compliance audit entry for a sensitive business operation.</summary>
    public interface IAuditLogger
    {
        Task LogAsync(
            string entityName,
            Guid? entityId,
            string action,
            object? oldValues = null,
            object? newValues = null,
            CancellationToken ct = default);
    }
}

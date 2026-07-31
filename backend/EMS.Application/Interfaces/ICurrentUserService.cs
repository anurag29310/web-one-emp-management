using System;

namespace EMS.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }

        /// <summary>Null only for SuperAdmin-role callers, who sit above every tenant.</summary>
        Guid? CompanyId { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
    }
}

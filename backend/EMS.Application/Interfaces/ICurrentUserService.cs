using System;

namespace EMS.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
    }
}

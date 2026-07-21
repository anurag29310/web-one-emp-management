using EMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace EMS.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(claim, out var id) ? id : null;
            }
        }

        public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        public string? UserAgent => _httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();
    }
}

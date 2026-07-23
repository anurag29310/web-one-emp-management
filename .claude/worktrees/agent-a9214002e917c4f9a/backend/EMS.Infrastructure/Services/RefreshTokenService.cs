using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using System;

namespace EMS.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly TimeSpan _lifetime = TimeSpan.FromDays(30);

        public RefreshToken CreateRefreshToken(Guid userId)
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid().ToString("N"),
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.Add(_lifetime),
                IsRevoked = false
            };
        }
    }
}

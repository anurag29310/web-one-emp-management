using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _db;

        // Stores password-reset tokens in memory (dev only; replace with a DB table in production)
        private static readonly ConcurrentDictionary<string, (Guid UserId, DateTime ExpiresAtUtc)> _resetTokens = new();

        public AuthRepository(ApplicationDbContext db) => _db = db;

        public async Task<User?> GetByUsernameOrEmailAsync(string userNameOrEmail, CancellationToken ct = default) =>
            await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail, ct);

        public async Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default) =>
            await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

        public async Task AddUserAsync(User user, CancellationToken ct = default) =>
            await _db.Users.AddAsync(user, ct);

        public Task UpdatePasswordAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
            await _db.RefreshTokens.AddAsync(token, ct);

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default) =>
            await _db.RefreshTokens.Include(r => r.User).ThenInclude(u => u!.Role)
                .FirstOrDefaultAsync(r => r.Token == token, ct);

        public Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
        {
            token.IsRevoked = true;
            _db.RefreshTokens.Update(token);
            return Task.CompletedTask;
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default)
        {
            var tokens = await _db.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync(ct);

            foreach (var t in tokens)
                t.IsRevoked = true;
        }

        public Task<string> CreatePasswordResetTokenAsync(Guid userId, CancellationToken ct = default)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            _resetTokens[token] = (userId, DateTime.UtcNow.AddHours(1));
            return Task.FromResult(token);
        }

        public Task<Guid?> ValidatePasswordResetTokenAsync(string token, CancellationToken ct = default)
        {
            if (_resetTokens.TryGetValue(token, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
                return Task.FromResult<Guid?>(entry.UserId);

            return Task.FromResult<Guid?>(null);
        }

        public Task InvalidatePasswordResetTokenAsync(string token, CancellationToken ct = default)
        {
            _resetTokens.TryRemove(token, out _);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EMS.Domain.Entities;

namespace EMS.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetByUsernameOrEmailAsync(string userNameOrEmail, CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);
        Task AddUserAsync(User user, CancellationToken ct = default);
        Task UpdatePasswordAsync(User user, CancellationToken ct = default);
        Task UpdateUserAsync(User user, CancellationToken ct = default);

        Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
        Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default);
        Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
        Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default);

        Task<string> CreatePasswordResetTokenAsync(Guid userId, CancellationToken ct = default);
        Task<Guid?> ValidatePasswordResetTokenAsync(string token, CancellationToken ct = default);
        Task InvalidatePasswordResetTokenAsync(string token, CancellationToken ct = default);

        Task AddMfaChallengeAsync(MfaChallenge challenge, CancellationToken ct = default);
        Task<MfaChallenge?> GetMfaChallengeAsync(Guid id, CancellationToken ct = default);
        Task ConsumeMfaChallengeAsync(MfaChallenge challenge, CancellationToken ct = default);

        Task AddMfaRecoveryCodesAsync(IEnumerable<MfaRecoveryCode> codes, CancellationToken ct = default);
        Task<IReadOnlyList<MfaRecoveryCode>> GetUnusedMfaRecoveryCodesAsync(Guid userId, CancellationToken ct = default);
        Task MarkMfaRecoveryCodeUsedAsync(MfaRecoveryCode code, CancellationToken ct = default);
        Task ReplaceMfaRecoveryCodesAsync(Guid userId, IEnumerable<MfaRecoveryCode> newCodes, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

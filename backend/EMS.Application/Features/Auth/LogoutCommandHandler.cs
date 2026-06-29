using EMS.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class LogoutCommandHandler
    {
        private readonly IAuthRepository _repo;

        public LogoutCommandHandler(IAuthRepository repo) => _repo = repo;

        public async Task Handle(LogoutCommand cmd, CancellationToken ct = default)
        {
            var token = await _repo.GetRefreshTokenAsync(cmd.RefreshToken, ct);
            if (token == null || token.IsRevoked)
                return; // idempotent

            await _repo.RevokeRefreshTokenAsync(token, ct);
            await _repo.SaveChangesAsync(ct);
        }
    }
}

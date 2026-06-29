using EMS.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class LogoutAllCommandHandler
    {
        private readonly IAuthRepository _repo;

        public LogoutAllCommandHandler(IAuthRepository repo) => _repo = repo;

        public async Task Handle(LogoutAllCommand cmd, CancellationToken ct = default)
        {
            await _repo.RevokeAllRefreshTokensAsync(cmd.UserId, ct);
            await _repo.SaveChangesAsync(ct);
        }
    }
}

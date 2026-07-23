using EMS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Auth
{
    public class GetCurrentUserQueryHandler
    {
        private readonly IAuthRepository _repo;

        public GetCurrentUserQueryHandler(IAuthRepository repo) => _repo = repo;

        public async Task<CurrentUserDto> Handle(GetCurrentUserQuery query, CancellationToken ct = default)
        {
            var user = await _repo.GetByIdAsync(query.UserId, ct);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            return new CurrentUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role?.Name,
                IsActive = user.IsActive,
                IsMfaEnabled = user.IsMfaEnabled
            };
        }
    }
}

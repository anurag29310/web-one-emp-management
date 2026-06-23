using System;
using System.Threading.Tasks;
using EMS.Domain.Entities;

namespace EMS.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetByUsernameOrEmailAsync(string userNameOrEmail);
        Task AddUserAsync(User user);
        Task AddRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task RevokeRefreshTokenAsync(RefreshToken token);
        Task SaveChangesAsync();
    }
}

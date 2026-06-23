using EMS.Domain.Entities;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        RefreshToken CreateRefreshToken(Guid userId);
    }
}

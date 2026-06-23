using EMS.Domain.Entities;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);
    }
}

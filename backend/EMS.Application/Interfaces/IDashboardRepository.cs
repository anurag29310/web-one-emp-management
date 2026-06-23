using EMS.Application.DTOs;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryDto> GetSummaryAsync();
    }
}

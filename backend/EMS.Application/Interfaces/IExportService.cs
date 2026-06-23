using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> GenerateCsvAsync(string filename, string csvContent);
    }
}

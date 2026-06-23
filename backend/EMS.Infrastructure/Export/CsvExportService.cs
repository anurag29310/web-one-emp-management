using EMS.Application.Interfaces;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Export
{
    public class CsvExportService : IExportService
    {
        public Task<byte[]> GenerateCsvAsync(string filename, string csvContent)
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            return Task.FromResult(bytes);
        }
    }
}

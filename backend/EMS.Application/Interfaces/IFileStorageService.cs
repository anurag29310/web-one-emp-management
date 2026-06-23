using System.IO;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(string container, string path, byte[] content, string contentType);
        Task<byte[]?> GetFileAsync(string container, string path);
        Task DeleteFileAsync(string container, string path);
    }
}

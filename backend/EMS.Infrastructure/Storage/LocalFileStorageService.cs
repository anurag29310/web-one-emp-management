using EMS.Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;

        public LocalFileStorageService(string basePath)
        {
            _basePath = Path.Combine(basePath, "Storage");
            if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
        }

        public async Task DeleteFileAsync(string container, string path)
        {
            var full = GetFullPath(container, path);
            if (File.Exists(full)) File.Delete(full);
            await Task.CompletedTask;
        }

        public async Task<byte[]?> GetFileAsync(string container, string path)
        {
            var full = GetFullPath(container, path);
            if (!File.Exists(full)) return null;
            return await File.ReadAllBytesAsync(full);
        }

        public async Task<string> SaveFileAsync(string container, string path, byte[] content, string contentType)
        {
            var dir = Path.GetDirectoryName(GetFullPath(container, path));
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            var full = GetFullPath(container, path);
            await File.WriteAllBytesAsync(full, content);
            return full;
        }

        private string GetFullPath(string container, string path)
        {
            var safeContainer = container.Replace("..", "").Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var safePath = path.Replace("..", "").Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(_basePath, safeContainer, safePath);
        }
    }
}

using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Documents.Handlers
{
    public class DownloadDocumentQueryHandler : IRequestHandler<Queries.DownloadDocumentQuery, Queries.DownloadDocumentResult>
    {
        private readonly IDocumentRepository _repo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<DownloadDocumentQueryHandler> _logger;

        public DownloadDocumentQueryHandler(IDocumentRepository repo, IFileStorageService storage, ILogger<DownloadDocumentQueryHandler> logger)
        {
            _repo = repo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<Queries.DownloadDocumentResult> Handle(Queries.DownloadDocumentQuery request, CancellationToken cancellationToken)
        {
            var doc = await _repo.GetByIdAsync(request.DocumentId);
            if (doc == null) return null!;

            var bytes = await _storage.GetFileAsync(doc.BlobContainer, doc.BlobPath);
            if (bytes == null) throw new Exception("File not found in storage");

            return new Queries.DownloadDocumentResult
            {
                Content = bytes,
                ContentType = doc.ContentType,
                FileName = doc.OriginalFileName
            };
        }
    }
}

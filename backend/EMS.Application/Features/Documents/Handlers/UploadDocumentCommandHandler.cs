using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Documents.Handlers
{
    public class UploadDocumentCommandHandler : IRequestHandler<Commands.UploadDocumentCommand, Guid>
    {
        private readonly IDocumentRepository _repo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<UploadDocumentCommandHandler> _logger;

        private static readonly string[] AllowedContentTypes = new[] { "application/pdf", "image/jpeg", "image/png", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

        public UploadDocumentCommandHandler(IDocumentRepository repo, IFileStorageService storage, ILogger<UploadDocumentCommandHandler> logger)
        {
            _repo = repo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<Guid> Handle(Commands.UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            if (request.Content == null || request.Content.Length == 0)
                throw new ArgumentException("File content is empty");
            if (request.Content.Length > MaxBytes)
                throw new ArgumentException("File exceeds maximum allowed size");
            if (Array.IndexOf(AllowedContentTypes, request.ContentType) < 0)
                throw new ArgumentException("Unsupported file type");

            var doc = new EmployeeDocument
            {
                Id = Guid.NewGuid(),
                EmployeeId = request.EmployeeId,
                DocumentType = request.DocumentType,
                OriginalFileName = request.FileName,
                ContentType = request.ContentType,
                FileSizeBytes = request.Content.Length,
                UploadedAtUtc = DateTime.UtcNow,
                UploadedBy = request.UploadedBy,
                ExpiresAtUtc = request.ExpiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = request.UploadedBy
            };

            // Save file to storage
            var container = "employee-documents";
            var blobPath = $"{request.EmployeeId}/{doc.Id}/{request.FileName}";
            await _storage.SaveFileAsync(container, blobPath, request.Content, request.ContentType);

            doc.BlobContainer = container;
            doc.BlobPath = blobPath;

            await _repo.AddAsync(doc);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Uploaded document {DocumentId} for employee {EmployeeId}", doc.Id, doc.EmployeeId);

            return doc.Id;
        }
    }
}

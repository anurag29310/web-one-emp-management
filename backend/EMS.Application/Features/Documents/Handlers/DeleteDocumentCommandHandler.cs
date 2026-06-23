using EMS.Application.Features.Documents.Commands;
using EMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Documents.Handlers
{
    public class DeleteDocumentCommandHandler : IRequestHandler<Commands.DeleteDocumentCommand>
    {
        private readonly IDocumentRepository _repo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<DeleteDocumentCommandHandler> _logger;

        public DeleteDocumentCommandHandler(IDocumentRepository repo, IFileStorageService storage, ILogger<DeleteDocumentCommandHandler> logger)
        {
            _repo = repo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<Unit> Handle(Commands.DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            var doc = await _repo.GetByIdAsync(request.DocumentId);
            if (doc == null) throw new Exception("Document not found");

            await _repo.SoftDeleteAsync(doc);
            await _repo.SaveChangesAsync();

            // attempt to remove file from storage silently
            try
            {
                await _storage.DeleteFileAsync(doc.BlobContainer, doc.BlobPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file for document {DocumentId}", doc.Id);
            }

            return Unit.Value;
        }

        Task IRequestHandler<DeleteDocumentCommand>.Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            return Handle(request, cancellationToken);
        }
    }
}

using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Recruitment.Handlers
{
    public class UploadCandidateAttachmentCommandHandler : IRequestHandler<UploadCandidateAttachmentCommand, Guid>
    {
        private readonly IRecruitmentRepository _repo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<UploadCandidateAttachmentCommandHandler> _logger;

        public UploadCandidateAttachmentCommandHandler(IRecruitmentRepository repo, IFileStorageService storage, ILogger<UploadCandidateAttachmentCommandHandler> logger)
        {
            _repo = repo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<Guid> Handle(UploadCandidateAttachmentCommand request, CancellationToken cancellationToken)
        {
            var candidate = await _repo.GetByIdAsync(request.CandidateId, cancellationToken)
                ?? throw new InvalidOperationException($"Candidate {request.CandidateId} not found.");

            var attachment = new CandidateAttachment
            {
                Id = Guid.NewGuid(),
                CandidateId = request.CandidateId,
                OriginalFileName = request.FileName,
                ContentType = request.ContentType,
                FileSizeBytes = request.Content.Length,
                UploadedAtUtc = DateTime.UtcNow,
                UploadedBy = request.RequestingUserId
            };

            var container = "candidate-attachments";
            var blobPath = $"{request.CandidateId}/{attachment.Id}/{request.FileName}";
            await _storage.SaveFileAsync(container, blobPath, request.Content, request.ContentType);

            attachment.BlobContainer = container;
            attachment.BlobPath = blobPath;

            await _repo.AddAttachmentAsync(attachment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Uploaded attachment {AttachmentId} for candidate {CandidateId}", attachment.Id, candidate.Id);

            return attachment.Id;
        }
    }
}

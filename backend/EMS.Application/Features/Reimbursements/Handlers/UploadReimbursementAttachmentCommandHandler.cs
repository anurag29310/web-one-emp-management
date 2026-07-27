using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class UploadReimbursementAttachmentCommandHandler : IRequestHandler<UploadReimbursementAttachmentCommand, Guid>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<UploadReimbursementAttachmentCommandHandler> _logger;

        public UploadReimbursementAttachmentCommandHandler(IReimbursementRepository repo, IAuthRepository authRepo, IFileStorageService storage, ILogger<UploadReimbursementAttachmentCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<Guid> Handle(UploadReimbursementAttachmentCommand request, CancellationToken cancellationToken)
        {
            var reimbursement = await _repo.GetByIdAsync(request.ReimbursementId, cancellationToken)
                ?? throw new InvalidOperationException($"Reimbursement {request.ReimbursementId} not found.");

            var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                throw new UnauthorizedAccessException("You can only attach documents to your own reimbursements.");

            if (reimbursement.Status is ReimbursementStatus.Approved or ReimbursementStatus.Rejected or ReimbursementStatus.Paid)
                throw new InvalidOperationException($"Reimbursement {reimbursement.ReimbursementNumber} is {reimbursement.Status} and no longer accepts new attachments.");

            var attachment = new ReimbursementAttachment
            {
                Id = Guid.NewGuid(),
                ReimbursementId = request.ReimbursementId,
                OriginalFileName = request.FileName,
                ContentType = request.ContentType,
                FileSizeBytes = request.Content.Length,
                UploadedAtUtc = DateTime.UtcNow,
                UploadedBy = request.RequestingUserId
            };

            var container = "reimbursement-attachments";
            var blobPath = $"{request.ReimbursementId}/{attachment.Id}/{request.FileName}";
            await _storage.SaveFileAsync(container, blobPath, request.Content, request.ContentType);

            attachment.BlobContainer = container;
            attachment.BlobPath = blobPath;

            await _repo.AddAttachmentAsync(attachment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Uploaded attachment {AttachmentId} for reimbursement {ReimbursementId}", attachment.Id, reimbursement.Id);

            return attachment.Id;
        }
    }
}

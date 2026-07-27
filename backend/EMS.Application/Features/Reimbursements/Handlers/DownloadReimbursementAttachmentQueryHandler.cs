using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Reimbursements.Handlers
{
    public class DownloadReimbursementAttachmentQueryHandler : IRequestHandler<DownloadReimbursementAttachmentQuery, DownloadReimbursementAttachmentResult?>
    {
        private readonly IReimbursementRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IFileStorageService _storage;

        public DownloadReimbursementAttachmentQueryHandler(IReimbursementRepository repo, IAuthRepository authRepo, IFileStorageService storage)
        {
            _repo = repo;
            _authRepo = authRepo;
            _storage = storage;
        }

        public async Task<DownloadReimbursementAttachmentResult?> Handle(DownloadReimbursementAttachmentQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _repo.GetAttachmentByIdAsync(request.AttachmentId, cancellationToken);
            if (attachment == null) return null;

            var reimbursement = await _repo.GetByIdAsync(attachment.ReimbursementId, cancellationToken);
            if (reimbursement == null) return null;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != reimbursement.EmployeeId)
                    return null;
            }

            var content = await _storage.GetFileAsync(attachment.BlobContainer, attachment.BlobPath);
            if (content == null) return null;

            return new DownloadReimbursementAttachmentResult
            {
                Content = content,
                ContentType = attachment.ContentType,
                FileName = attachment.OriginalFileName
            };
        }
    }
}

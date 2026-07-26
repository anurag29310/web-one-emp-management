using EMS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class DownloadTaskAttachmentQueryHandler : IRequestHandler<DownloadTaskAttachmentQuery, DownloadTaskAttachmentResult?>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IFileStorageService _storage;

        public DownloadTaskAttachmentQueryHandler(ITaskRepository repo, IAuthRepository authRepo, IFileStorageService storage)
        {
            _repo = repo;
            _authRepo = authRepo;
            _storage = storage;
        }

        public async Task<DownloadTaskAttachmentResult?> Handle(DownloadTaskAttachmentQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _repo.GetAttachmentByIdAsync(request.AttachmentId, cancellationToken);
            if (attachment == null) return null;

            var task = await _repo.GetByIdAsync(attachment.TaskId, cancellationToken);
            if (task == null) return null;

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    return null;
            }

            var content = await _storage.GetFileAsync(attachment.BlobContainer, attachment.BlobPath);
            if (content == null) return null;

            return new DownloadTaskAttachmentResult
            {
                Content = content,
                ContentType = attachment.ContentType,
                FileName = attachment.OriginalFileName
            };
        }
    }
}

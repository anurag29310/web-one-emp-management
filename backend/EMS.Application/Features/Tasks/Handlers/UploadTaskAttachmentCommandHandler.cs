using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Tasks.Handlers
{
    public class UploadTaskAttachmentCommandHandler : IRequestHandler<UploadTaskAttachmentCommand, Guid>
    {
        private readonly ITaskRepository _repo;
        private readonly IAuthRepository _authRepo;
        private readonly IFileStorageService _storage;
        private readonly ILogger<UploadTaskAttachmentCommandHandler> _logger;

        public UploadTaskAttachmentCommandHandler(ITaskRepository repo, IAuthRepository authRepo, IFileStorageService storage, ILogger<UploadTaskAttachmentCommandHandler> logger)
        {
            _repo = repo;
            _authRepo = authRepo;
            _storage = storage;
            _logger = logger;
        }

        public async Task<Guid> Handle(UploadTaskAttachmentCommand request, CancellationToken cancellationToken)
        {
            var task = await _repo.GetByIdAsync(request.TaskId, cancellationToken)
                ?? throw new InvalidOperationException($"Task {request.TaskId} not found.");

            if (!request.IsPrivileged)
            {
                var requester = await _authRepo.GetByIdAsync(request.RequestingUserId, cancellationToken);
                if (requester?.EmployeeId == null || requester.EmployeeId != task.AssignedEmployeeId)
                    throw new UnauthorizedAccessException("You can only upload attachments to tasks assigned to you.");
            }

            if (task.Status is TaskItemStatus.Completed or TaskItemStatus.Cancelled)
                throw new InvalidOperationException($"Task {task.TaskNumber} is {task.Status} and is read-only.");

            var attachment = new TaskAttachment
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                OriginalFileName = request.FileName,
                ContentType = request.ContentType,
                FileSizeBytes = request.Content.Length,
                UploadedAtUtc = DateTime.UtcNow,
                UploadedBy = request.RequestingUserId
            };

            var container = "task-attachments";
            var blobPath = $"{request.TaskId}/{attachment.Id}/{request.FileName}";
            await _storage.SaveFileAsync(container, blobPath, request.Content, request.ContentType);

            attachment.BlobContainer = container;
            attachment.BlobPath = blobPath;

            await _repo.AddAttachmentAsync(attachment, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Uploaded attachment {AttachmentId} for task {TaskId}", attachment.Id, task.Id);

            return attachment.Id;
        }
    }
}

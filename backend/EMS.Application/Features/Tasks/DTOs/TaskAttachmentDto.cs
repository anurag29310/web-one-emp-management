using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Tasks.DTOs
{
    public class TaskAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }

        public static TaskAttachmentDto FromEntity(TaskAttachment a) => new()
        {
            Id = a.Id,
            TaskId = a.TaskId,
            OriginalFileName = a.OriginalFileName,
            ContentType = a.ContentType,
            FileSizeBytes = a.FileSizeBytes,
            UploadedAtUtc = a.UploadedAtUtc,
            UploadedBy = a.UploadedBy
        };
    }
}

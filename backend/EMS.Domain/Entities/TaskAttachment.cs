using System;

namespace EMS.Domain.Entities
{
    /// <summary>Uploaded photo/document evidence for a task. Mirrors EmployeeDocument's shape, minus the fields that don't apply here (DocumentType, ExpiresAtUtc, soft delete).</summary>
    public class TaskAttachment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public string BlobContainer { get; set; } = null!;
        public string BlobPath { get; set; } = null!;
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }
    }
}

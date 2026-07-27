using System;

namespace EMS.Domain.Entities
{
    /// <summary>Uploaded receipt/supporting document for a reimbursement. Mirrors TaskAttachment/EmployeeDocument.</summary>
    public class ReimbursementAttachment
    {
        public Guid Id { get; set; }
        public Guid ReimbursementId { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public string BlobContainer { get; set; } = null!;
        public string BlobPath { get; set; } = null!;
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }
    }
}

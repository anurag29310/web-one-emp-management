using System;

namespace EMS.Domain.Entities
{
    /// <summary>Resume/supporting document for a candidate. Mirrors TaskAttachment's shape.</summary>
    public class CandidateAttachment
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public string BlobContainer { get; set; } = null!;
        public string BlobPath { get; set; } = null!;
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }
    }
}

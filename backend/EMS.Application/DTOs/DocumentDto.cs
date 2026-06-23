using System;

namespace EMS.Application.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string DocumentType { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }
}

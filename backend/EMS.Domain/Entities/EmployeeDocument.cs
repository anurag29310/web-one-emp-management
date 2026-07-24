using System;

namespace EMS.Domain.Entities
{
    public class EmployeeDocument
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string DocumentType { get; set; } = null!; // ID Proof, OfferLetter, NDA, Appraisal, Payslip, Other
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public string BlobContainer { get; set; } = null!;
        public string BlobPath { get; set; } = null!;
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }

        // audit / soft delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }

        public uint RowVersion { get; set; }
    }
}

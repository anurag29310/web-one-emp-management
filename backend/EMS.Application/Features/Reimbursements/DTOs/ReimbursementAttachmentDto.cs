using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Reimbursements.DTOs
{
    public class ReimbursementAttachmentDto
    {
        public Guid Id { get; set; }
        public Guid ReimbursementId { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public Guid? UploadedBy { get; set; }

        public static ReimbursementAttachmentDto FromEntity(ReimbursementAttachment a) => new()
        {
            Id = a.Id,
            ReimbursementId = a.ReimbursementId,
            OriginalFileName = a.OriginalFileName,
            ContentType = a.ContentType,
            FileSizeBytes = a.FileSizeBytes,
            UploadedAtUtc = a.UploadedAtUtc,
            UploadedBy = a.UploadedBy
        };
    }
}

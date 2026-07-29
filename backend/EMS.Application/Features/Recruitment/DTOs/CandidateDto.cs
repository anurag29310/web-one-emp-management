using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Recruitment.DTOs
{
    public class CandidateDto
    {
        public Guid Id { get; set; }
        public string CandidateNumber { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public Guid DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Source { get; set; }
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public Guid? ConvertedEmployeeId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public static CandidateDto FromEntity(Candidate c) => new()
        {
            Id = c.Id,
            CandidateNumber = c.CandidateNumber,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            DesignationId = c.DesignationId,
            DesignationName = c.Designation?.Name,
            DepartmentId = c.DepartmentId,
            DepartmentName = c.Department?.Name,
            Source = c.Source,
            AppliedDate = c.AppliedDate,
            Status = c.Status.ToString(),
            Notes = c.Notes,
            ConvertedEmployeeId = c.ConvertedEmployeeId,
            IsDeleted = c.IsDeleted,
            CreatedAtUtc = c.CreatedAtUtc,
            UpdatedAtUtc = c.UpdatedAtUtc
        };
    }

    public class CandidateAttachmentDto
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAtUtc { get; set; }

        public static CandidateAttachmentDto FromEntity(CandidateAttachment a) => new()
        {
            Id = a.Id,
            OriginalFileName = a.OriginalFileName,
            ContentType = a.ContentType,
            FileSizeBytes = a.FileSizeBytes,
            UploadedAtUtc = a.UploadedAtUtc
        };
    }
}

using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Recruitment.DTOs
{
    public class OfferDto
    {
        public Guid Id { get; set; }
        public string OfferNumber { get; set; } = null!;
        public Guid CandidateId { get; set; }
        public Guid DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public decimal OfferedSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? IssuedAtUtc { get; set; }
        public DateTime? RespondedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string? Notes { get; set; }
        public bool HasDocument { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public static OfferDto FromEntity(Offer o) => new()
        {
            Id = o.Id,
            OfferNumber = o.OfferNumber,
            CandidateId = o.CandidateId,
            DesignationId = o.DesignationId,
            DesignationName = o.Designation?.Name,
            DepartmentId = o.DepartmentId,
            DepartmentName = o.Department?.Name,
            OfferedSalary = o.OfferedSalary,
            JoiningDate = o.JoiningDate,
            Status = o.Status.ToString(),
            IssuedAtUtc = o.IssuedAtUtc,
            RespondedAtUtc = o.RespondedAtUtc,
            ExpiresAtUtc = o.ExpiresAtUtc,
            Notes = o.Notes,
            HasDocument = !string.IsNullOrEmpty(o.BlobContainer) && !string.IsNullOrEmpty(o.BlobPath),
            CreatedAtUtc = o.CreatedAtUtc
        };
    }
}

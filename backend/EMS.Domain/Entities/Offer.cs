using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class Offer
    {
        public Guid Id { get; set; }
        public string OfferNumber { get; set; } = null!;
        public Guid CandidateId { get; set; }
        public Candidate? Candidate { get; set; }
        public Guid DesignationId { get; set; }
        public Designation? Designation { get; set; }
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public decimal OfferedSalary { get; set; }
        public DateTime JoiningDate { get; set; }
        public OfferStatus Status { get; set; } = OfferStatus.Draft;

        /// <summary>Set when Status becomes Sent (the offer letter PDF is generated at this point).</summary>
        public DateTime? IssuedAtUtc { get; set; }

        /// <summary>Set when the candidate's Accept/Reject response is recorded.</summary>
        public DateTime? RespondedAtUtc { get; set; }

        /// <summary>Optional. When set and still in the past for a Sent offer, the daily sweep
        /// (RunDailySweepCommand) flips Status to Expired. Offers without one never auto-expire.</summary>
        public DateTime? ExpiresAtUtc { get; set; }
        public string? Notes { get; set; }
        public string? BlobContainer { get; set; }
        public string? BlobPath { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}

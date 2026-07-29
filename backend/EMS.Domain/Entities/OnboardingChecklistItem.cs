using System;

namespace EMS.Domain.Entities
{
    /// <summary>One "Joining Checklist" item for a candidate. A default set is auto-created when an
    /// Offer is Accepted; Admin/HR can add custom ones on top.</summary>
    public class OnboardingChecklistItem
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Candidate? Candidate { get; set; }
        public string ItemName { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public Guid? CompletedBy { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}

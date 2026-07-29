using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class Interview
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Candidate? Candidate { get; set; }
        public Guid InterviewerEmployeeId { get; set; }
        public Employee? InterviewerEmployee { get; set; }

        /// <summary>Free text (e.g. "Technical Round 1", "HR Round") — requirements.md doesn't enumerate a fixed round list.</summary>
        public string Round { get; set; } = null!;
        public InterviewMode Mode { get; set; }
        public DateTime ScheduledAtUtc { get; set; }
        public int? DurationMinutes { get; set; }
        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
        public string? Feedback { get; set; }

        /// <summary>1-5. Set together with Feedback/Outcome when the interviewer submits their review.</summary>
        public int? Rating { get; set; }
        public InterviewOutcome Outcome { get; set; } = InterviewOutcome.Pending;

        // No soft delete — deliberately, matching Tasks. There is no "Delete Interview" action;
        // Cancel/Reschedule are status transitions, not deletions.
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}

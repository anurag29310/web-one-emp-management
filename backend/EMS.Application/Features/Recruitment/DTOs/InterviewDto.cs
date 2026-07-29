using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Recruitment.DTOs
{
    public class InterviewDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public Guid InterviewerEmployeeId { get; set; }
        public string? InterviewerName { get; set; }
        public string Round { get; set; } = null!;
        public string Mode { get; set; } = null!;
        public DateTime ScheduledAtUtc { get; set; }
        public int? DurationMinutes { get; set; }
        public string Status { get; set; } = null!;
        public string? Feedback { get; set; }
        public int? Rating { get; set; }
        public string Outcome { get; set; } = null!;
        public DateTime CreatedAtUtc { get; set; }

        public static InterviewDto FromEntity(Interview i) => new()
        {
            Id = i.Id,
            CandidateId = i.CandidateId,
            InterviewerEmployeeId = i.InterviewerEmployeeId,
            InterviewerName = i.InterviewerEmployee != null ? $"{i.InterviewerEmployee.FirstName} {i.InterviewerEmployee.LastName}".Trim() : null,
            Round = i.Round,
            Mode = i.Mode.ToString(),
            ScheduledAtUtc = i.ScheduledAtUtc,
            DurationMinutes = i.DurationMinutes,
            Status = i.Status.ToString(),
            Feedback = i.Feedback,
            Rating = i.Rating,
            Outcome = i.Outcome.ToString(),
            CreatedAtUtc = i.CreatedAtUtc
        };
    }
}

using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Recruitment.DTOs
{
    public class OnboardingChecklistItemDto
    {
        public Guid Id { get; set; }
        public Guid CandidateId { get; set; }
        public string ItemName { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public static OnboardingChecklistItemDto FromEntity(OnboardingChecklistItem i) => new()
        {
            Id = i.Id,
            CandidateId = i.CandidateId,
            ItemName = i.ItemName,
            IsCompleted = i.IsCompleted,
            CompletedAtUtc = i.CompletedAtUtc,
            Notes = i.Notes,
            CreatedAtUtc = i.CreatedAtUtc
        };
    }
}

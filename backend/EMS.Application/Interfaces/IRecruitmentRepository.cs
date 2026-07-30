using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IRecruitmentRepository
    {
        // ─── Candidates ────────────────────────────────────────────────────────────
        Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Candidate?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Candidate>> GetAllAsync(int page, int pageSize, CandidateStatus? status, Guid? designationId, string? search, CancellationToken ct = default);
        Task<int> CountAsync(CandidateStatus? status, Guid? designationId, string? search, CancellationToken ct = default);
        Task<IEnumerable<Candidate>> GetAllForExportAsync(CandidateStatus? status, Guid? designationId, string? search, CancellationToken ct = default);
        Task AddAsync(Candidate candidate, CancellationToken ct = default);
        Task UpdateAsync(Candidate candidate, CancellationToken ct = default);
        Task DeleteAsync(Candidate candidate, CancellationToken ct = default);

        // ─── Attachments ───────────────────────────────────────────────────────────
        Task<IEnumerable<CandidateAttachment>> GetAttachmentsAsync(Guid candidateId, CancellationToken ct = default);
        Task<CandidateAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default);
        Task AddAttachmentAsync(CandidateAttachment attachment, CancellationToken ct = default);

        // ─── Interviews ────────────────────────────────────────────────────────────
        Task<Interview?> GetInterviewByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Interview>> GetInterviewsByCandidateAsync(Guid candidateId, CancellationToken ct = default);
        Task AddInterviewAsync(Interview interview, CancellationToken ct = default);
        Task UpdateInterviewAsync(Interview interview, CancellationToken ct = default);

        // ─── Offers ────────────────────────────────────────────────────────────────
        Task<Offer?> GetOfferByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Offer>> GetOffersByCandidateAsync(Guid candidateId, CancellationToken ct = default);
        Task<IEnumerable<Offer>> GetSentOffersPastExpiryAsync(DateTime asOfUtc, CancellationToken ct = default);
        Task AddOfferAsync(Offer offer, CancellationToken ct = default);
        Task UpdateOfferAsync(Offer offer, CancellationToken ct = default);

        // ─── Onboarding checklist ──────────────────────────────────────────────────
        Task<IEnumerable<OnboardingChecklistItem>> GetChecklistItemsAsync(Guid candidateId, CancellationToken ct = default);
        Task<OnboardingChecklistItem?> GetChecklistItemByIdAsync(Guid id, CancellationToken ct = default);
        Task AddChecklistItemAsync(OnboardingChecklistItem item, CancellationToken ct = default);
        Task UpdateChecklistItemAsync(OnboardingChecklistItem item, CancellationToken ct = default);

        Task SaveChangesAsync(CancellationToken ct = default);
    }
}

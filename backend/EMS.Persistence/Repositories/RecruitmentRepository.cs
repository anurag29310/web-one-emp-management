using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Persistence.Repositories
{
    public class RecruitmentRepository : IRecruitmentRepository
    {
        private readonly ApplicationDbContext _db;

        public RecruitmentRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        private IQueryable<Candidate> IncludeRelated(IQueryable<Candidate> q) =>
            q.Include(c => c.Designation).Include(c => c.Department);

        public async Task<Candidate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await IncludeRelated(_db.Candidates).FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

        public async Task<Candidate?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await IncludeRelated(_db.Candidates).FirstOrDefaultAsync(c => c.Id == id, ct);

        private IQueryable<Candidate> BuildFilterQuery(CandidateStatus? status, Guid? designationId, string? search)
        {
            var q = _db.Candidates.AsNoTracking().Where(c => !c.IsDeleted);

            if (status.HasValue)
                q = q.Where(c => c.Status == status.Value);
            if (designationId.HasValue)
                q = q.Where(c => c.DesignationId == designationId.Value);
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.FirstName.Contains(search) || c.LastName.Contains(search) || c.Email.Contains(search));

            return q;
        }

        public async Task<IEnumerable<Candidate>> GetAllAsync(int page, int pageSize, CandidateStatus? status, Guid? designationId, string? search, CancellationToken ct = default) =>
            await IncludeRelated(BuildFilterQuery(status, designationId, search))
                .OrderByDescending(c => c.AppliedDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(CandidateStatus? status, Guid? designationId, string? search, CancellationToken ct = default) =>
            await BuildFilterQuery(status, designationId, search).CountAsync(ct);

        public async Task<IEnumerable<Candidate>> GetAllForExportAsync(CandidateStatus? status, Guid? designationId, string? search, CancellationToken ct = default) =>
            await IncludeRelated(BuildFilterQuery(status, designationId, search))
                .OrderByDescending(c => c.AppliedDate)
                .ToListAsync(ct);

        public async Task AddAsync(Candidate candidate, CancellationToken ct = default) =>
            await _db.Candidates.AddAsync(candidate, ct);

        public Task UpdateAsync(Candidate candidate, CancellationToken ct = default)
        {
            _db.Candidates.Update(candidate);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Candidate candidate, CancellationToken ct = default)
        {
            candidate.IsDeleted = true;
            _db.Candidates.Update(candidate);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<CandidateAttachment>> GetAttachmentsAsync(Guid candidateId, CancellationToken ct = default) =>
            await _db.CandidateAttachments.AsNoTracking()
                .Where(a => a.CandidateId == candidateId)
                .OrderBy(a => a.UploadedAtUtc)
                .ToListAsync(ct);

        public async Task<CandidateAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default) =>
            await _db.CandidateAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        public async Task AddAttachmentAsync(CandidateAttachment attachment, CancellationToken ct = default) =>
            await _db.CandidateAttachments.AddAsync(attachment, ct);

        private IQueryable<Interview> IncludeInterviewRelated(IQueryable<Interview> q) =>
            q.Include(i => i.Candidate).Include(i => i.InterviewerEmployee);

        public async Task<Interview?> GetInterviewByIdAsync(Guid id, CancellationToken ct = default) =>
            await IncludeInterviewRelated(_db.Interviews).FirstOrDefaultAsync(i => i.Id == id, ct);

        public async Task<IEnumerable<Interview>> GetInterviewsByCandidateAsync(Guid candidateId, CancellationToken ct = default) =>
            await IncludeInterviewRelated(_db.Interviews.AsNoTracking())
                .Where(i => i.CandidateId == candidateId)
                .OrderBy(i => i.ScheduledAtUtc)
                .ToListAsync(ct);

        public async Task AddInterviewAsync(Interview interview, CancellationToken ct = default) =>
            await _db.Interviews.AddAsync(interview, ct);

        public Task UpdateInterviewAsync(Interview interview, CancellationToken ct = default)
        {
            _db.Interviews.Update(interview);
            return Task.CompletedTask;
        }

        public async Task<Offer?> GetOfferByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Offers.Include(o => o.Candidate).Include(o => o.Designation).Include(o => o.Department)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

        public async Task<IEnumerable<Offer>> GetOffersByCandidateAsync(Guid candidateId, CancellationToken ct = default) =>
            await _db.Offers.AsNoTracking().Include(o => o.Designation).Include(o => o.Department)
                .Where(o => o.CandidateId == candidateId)
                .OrderByDescending(o => o.CreatedAtUtc)
                .ToListAsync(ct);

        public async Task<IEnumerable<Offer>> GetSentOffersPastExpiryAsync(DateTime asOfUtc, CancellationToken ct = default) =>
            await _db.Offers
                .Where(o => o.Status == OfferStatus.Sent && o.ExpiresAtUtc != null && o.ExpiresAtUtc <= asOfUtc)
                .ToListAsync(ct);

        public async Task AddOfferAsync(Offer offer, CancellationToken ct = default) =>
            await _db.Offers.AddAsync(offer, ct);

        public Task UpdateOfferAsync(Offer offer, CancellationToken ct = default)
        {
            _db.Offers.Update(offer);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<OnboardingChecklistItem>> GetChecklistItemsAsync(Guid candidateId, CancellationToken ct = default) =>
            await _db.OnboardingChecklistItems.AsNoTracking()
                .Where(i => i.CandidateId == candidateId)
                .OrderBy(i => i.CreatedAtUtc)
                .ToListAsync(ct);

        public async Task<OnboardingChecklistItem?> GetChecklistItemByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.OnboardingChecklistItems.FirstOrDefaultAsync(i => i.Id == id, ct);

        public async Task AddChecklistItemAsync(OnboardingChecklistItem item, CancellationToken ct = default) =>
            await _db.OnboardingChecklistItems.AddAsync(item, ct);

        public Task UpdateChecklistItemAsync(OnboardingChecklistItem item, CancellationToken ct = default)
        {
            _db.OnboardingChecklistItems.Update(item);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);
    }
}

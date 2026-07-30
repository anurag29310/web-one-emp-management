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
    public class ReimbursementRepository : IReimbursementRepository
    {
        private readonly ApplicationDbContext _db;

        public ReimbursementRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        private IQueryable<Reimbursement> IncludeRelated(IQueryable<Reimbursement> q) => q.Include(r => r.Employee);

        public async Task<Reimbursement?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await IncludeRelated(_db.Reimbursements).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

        public async Task<Reimbursement?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default) =>
            await IncludeRelated(_db.Reimbursements).FirstOrDefaultAsync(r => r.Id == id, ct);

        private IQueryable<Reimbursement> BuildFilterQuery(Guid? employeeId, ReimbursementStatus? status)
        {
            var q = _db.Reimbursements.AsNoTracking().Where(r => !r.IsDeleted);

            if (employeeId.HasValue)
                q = q.Where(r => r.EmployeeId == employeeId.Value);
            if (status.HasValue)
                q = q.Where(r => r.Status == status.Value);

            return q;
        }

        public async Task<IEnumerable<Reimbursement>> GetAllAsync(int page, int pageSize, Guid? employeeId, ReimbursementStatus? status, CancellationToken ct = default) =>
            await IncludeRelated(BuildFilterQuery(employeeId, status))
                .OrderByDescending(r => r.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

        public async Task<int> CountAsync(Guid? employeeId, ReimbursementStatus? status, CancellationToken ct = default) =>
            await BuildFilterQuery(employeeId, status).CountAsync(ct);

        public async Task<IEnumerable<Reimbursement>> GetAllForExportAsync(Guid? employeeId, ReimbursementStatus? status, CancellationToken ct = default) =>
            await IncludeRelated(BuildFilterQuery(employeeId, status))
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync(ct);

        public async Task AddAsync(Reimbursement reimbursement, CancellationToken ct = default) =>
            await _db.Reimbursements.AddAsync(reimbursement, ct);

        public Task UpdateAsync(Reimbursement reimbursement, CancellationToken ct = default)
        {
            _db.Reimbursements.Update(reimbursement);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default) =>
            await _db.SaveChangesAsync(ct);

        // Tracked (not AsNoTracking) — Payroll mutates and saves these directly after reading them,
        // so they need to already be attached to this unit of work rather than requiring a second
        // fetch-and-reattach round trip.
        public async Task<IEnumerable<Reimbursement>> GetApprovedUnprocessedByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
            await _db.Reimbursements
                .Where(r => !r.IsDeleted && r.EmployeeId == employeeId && r.Status == ReimbursementStatus.Approved && !r.PayrollProcessed)
                .ToListAsync(ct);

        public async Task<IEnumerable<ReimbursementAttachment>> GetAttachmentsAsync(Guid reimbursementId, CancellationToken ct = default) =>
            await _db.ReimbursementAttachments.AsNoTracking()
                .Where(a => a.ReimbursementId == reimbursementId)
                .OrderBy(a => a.UploadedAtUtc)
                .ToListAsync(ct);

        public async Task<ReimbursementAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default) =>
            await _db.ReimbursementAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        public async Task AddAttachmentAsync(ReimbursementAttachment attachment, CancellationToken ct = default) =>
            await _db.ReimbursementAttachments.AddAsync(attachment, ct);
    }
}

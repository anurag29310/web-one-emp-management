using EMS.Domain.Entities;
using EMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Interfaces
{
    public interface IReimbursementRepository
    {
        Task<Reimbursement?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Reimbursement?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Reimbursement>> GetAllAsync(
            int page, int pageSize, Guid? employeeId, ReimbursementStatus? status, CancellationToken ct = default);
        Task<int> CountAsync(Guid? employeeId, ReimbursementStatus? status, CancellationToken ct = default);
        Task AddAsync(Reimbursement reimbursement, CancellationToken ct = default);
        Task UpdateAsync(Reimbursement reimbursement, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);

        /// <summary>Approved, not-yet-processed reimbursements for an employee — what the next payroll run should fold in.</summary>
        Task<IEnumerable<Reimbursement>> GetApprovedUnprocessedByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

        Task<IEnumerable<ReimbursementAttachment>> GetAttachmentsAsync(Guid reimbursementId, CancellationToken ct = default);
        Task<ReimbursementAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken ct = default);
        Task AddAttachmentAsync(ReimbursementAttachment attachment, CancellationToken ct = default);
    }
}

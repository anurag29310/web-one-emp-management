using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class Candidate
    {
        public Guid Id { get; set; }
        public string CandidateNumber { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }

        /// <summary>Position applied for.</summary>
        public Guid DesignationId { get; set; }
        public Designation? Designation { get; set; }
        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        /// <summary>Free text — requirements.md doesn't enumerate a fixed source list (e.g. Referral, Job Portal, LinkedIn), so this isn't an enum, matching Reimbursement.ExpenseCategory's precedent.</summary>
        public string? Source { get; set; }
        public DateTime AppliedDate { get; set; }
        public CandidateStatus Status { get; set; } = CandidateStatus.Applied;
        public string? Notes { get; set; }

        /// <summary>Set once Status becomes Hired via the Convert-to-Employee action.</summary>
        public Guid? ConvertedEmployeeId { get; set; }
        public Employee? ConvertedEmployee { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public uint RowVersion { get; set; }
    }
}

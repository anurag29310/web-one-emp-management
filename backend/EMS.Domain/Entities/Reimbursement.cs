using EMS.Domain.Enums;
using System;

namespace EMS.Domain.Entities
{
    public class Reimbursement
    {
        public Guid Id { get; set; }

        /// <summary>Human-readable identifier, derived from Id (e.g. "REI-3F2A9B10"). Not user-editable.</summary>
        public string ReimbursementNumber { get; set; } = null!;

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public string ExpenseTitle { get; set; } = null!;
        public string ExpenseCategory { get; set; } = null!;
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Description { get; set; }
        public string? Notes { get; set; }

        /// <summary>Set only for mileage claims. When present, Amount is computed server-side as
        /// DistanceKm * MileageRatePerKm (the rate in effect at submission time, frozen here so a later
        /// rate change never retroactively changes an already-submitted claim) — the client-supplied
        /// Amount is ignored for these claims.</summary>
        public decimal? DistanceKm { get; set; }
        public decimal? MileageRatePerKm { get; set; }

        public ReimbursementStatus Status { get; set; } = ReimbursementStatus.Draft;

        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public Guid? ApprovedBy { get; set; }

        /// <summary>Set on Reject/Request Changes — "View approval remarks" for the employee.</summary>
        public string? ReviewRemarks { get; set; }

        /// <summary>Set once an Approved reimbursement has been folded into a payroll run; prevents double payment.</summary>
        public bool PayrollProcessed { get; set; }
        public Guid? PayrollRunId { get; set; }
        public DateTime? PayrollDate { get; set; }

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

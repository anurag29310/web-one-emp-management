using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities
{
    public class PayrollRun
    {
        public Guid Id { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime ProcessedAtUtc { get; set; }
        public Guid ProcessedBy { get; set; }
        public IEnumerable<Payslip>? Payslips { get; set; }
        public string? Status { get; set; }

        // CreatedAtUtc/CreatedBy are intentionally omitted — ProcessedAtUtc/ProcessedBy above already
        // record that same creation event (a PayrollRun's only "created by" is Process Payroll).
        // UpdatedAtUtc/UpdatedBy track the one other lifecycle event a run has: Approve.
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
    }
}

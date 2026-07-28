using System;

namespace EMS.Domain.Entities
{
    public class Payslip
    {
        public Guid Id { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public decimal Basic { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }

        /// <summary>Sum of the employee's Approved, not-yet-processed reimbursements as of this run. Added to NetPay, not GrossPay — reimbursements are expense repayments, not taxable earnings.</summary>
        public decimal TotalReimbursements { get; set; }

        /// <summary>Discretionary bonus for this run. Manual-entry only, via ProcessPayrollCommand.Adjustments — there's no basis to auto-calculate a discretionary amount. Included in GrossPay (taxable earnings).</summary>
        public decimal TotalBonus { get; set; }

        /// <summary>Overtime pay for this run. Auto-calculated from Attendance vs. the employee's assigned shift by default, but a per-employee Adjustments override always wins. Included in GrossPay (taxable earnings).</summary>
        public decimal TotalOvertime { get; set; }

        /// <summary>Overtime hours behind TotalOvertime when auto-calculated; 0 when TotalOvertime came from a manual override (the override is an amount, not a derived hour count).</summary>
        public decimal OvertimeHours { get; set; }

        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public string? BlobPath { get; set; }
        public string? BlobContainer { get; set; }
    }
}

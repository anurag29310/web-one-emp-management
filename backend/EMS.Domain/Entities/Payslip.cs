using System;

namespace EMS.Domain.Entities
{
    public class Payslip
    {
        public Guid Id { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid EmployeeId { get; set; }
        public decimal Basic { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }

        /// <summary>Sum of the employee's Approved, not-yet-processed reimbursements as of this run. Added to NetPay, not GrossPay — reimbursements are expense repayments, not taxable earnings.</summary>
        public decimal TotalReimbursements { get; set; }

        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public string? BlobPath { get; set; }
        public string? BlobContainer { get; set; }
    }
}

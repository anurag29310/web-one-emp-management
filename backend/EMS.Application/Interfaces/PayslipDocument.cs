using System;
using System.Collections.Generic;

namespace EMS.Application.Interfaces
{
    // Rendering input for IPdfService — deliberately separate from the persisted Payslip entity
    // (which only stores totals) so the PDF can show an itemized breakdown without changing the
    // database schema.
    public class PayslipDocument
    {
        public string EmployeeName { get; set; } = null!;
        public string EmployeeCode { get; set; } = null!;
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public decimal Basic { get; set; }
        public IReadOnlyList<PayslipLineItem> Allowances { get; set; } = Array.Empty<PayslipLineItem>();
        public IReadOnlyList<PayslipLineItem> Deductions { get; set; } = Array.Empty<PayslipLineItem>();
        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
    }

    public class PayslipLineItem
    {
        public string Name { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}

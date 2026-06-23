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
    }
}

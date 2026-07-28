using EMS.Application.Features.Payroll.Commands;
using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Queries
{
    public class DryRunPayrollQuery : IRequest<IEnumerable<PayslipPreview>>
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        /// <summary>Same shape and semantics as ProcessPayrollCommand.Adjustments — supplying the
        /// same list here previews exactly what Process would produce.</summary>
        public List<PayrollAdjustment>? Adjustments { get; set; }
    }

    public class PayslipPreview
    {
        public Guid EmployeeId { get; set; }
        public decimal Basic { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalReimbursements { get; set; }
        public decimal TotalBonus { get; set; }
        public decimal TotalOvertime { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
    }
}

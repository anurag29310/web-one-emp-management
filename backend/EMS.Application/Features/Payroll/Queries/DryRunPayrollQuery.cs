using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Queries
{
    public class DryRunPayrollQuery : IRequest<IEnumerable<PayslipPreview>>
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class PayslipPreview
    {
        public Guid EmployeeId { get; set; }
        public decimal Basic { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
    }
}

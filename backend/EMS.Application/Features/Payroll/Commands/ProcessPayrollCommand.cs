using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Commands
{
    public class ProcessPayrollCommand : IRequest<Guid>
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public Guid ProcessedBy { get; set; }

        /// <summary>Per-employee overrides for this run. Bonus is manual-only (there's no basis to
        /// auto-calculate a discretionary amount) — supply it here to include one. Overtime is
        /// auto-calculated from Attendance vs. the employee's shift by default; supply
        /// <see cref="PayrollAdjustment.OvertimeAmount"/> here to override the auto-calculated
        /// value for an employee when it's wrong. Employees not listed get Bonus = 0 and
        /// auto-calculated Overtime.</summary>
        public List<PayrollAdjustment>? Adjustments { get; set; }
    }

    public class PayrollAdjustment
    {
        public Guid EmployeeId { get; set; }
        public decimal? BonusAmount { get; set; }
        public decimal? OvertimeAmount { get; set; }
    }
}

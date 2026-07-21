using EMS.Domain.Entities;
using System;

namespace EMS.Application.Features.Payroll.Dtos
{
    public class PayslipDto
    {
        public Guid Id { get; set; }
        public Guid PayrollRunId { get; set; }
        public Guid EmployeeId { get; set; }
        public decimal Basic { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
        public DateTime GeneratedAtUtc { get; set; }
        public bool HasDocument { get; set; }

        public static PayslipDto FromEntity(Payslip p) => new()
        {
            Id = p.Id,
            PayrollRunId = p.PayrollRunId,
            EmployeeId = p.EmployeeId,
            Basic = p.Basic,
            TotalAllowances = p.TotalAllowances,
            TotalDeductions = p.TotalDeductions,
            GrossPay = p.GrossPay,
            NetPay = p.NetPay,
            GeneratedAtUtc = p.GeneratedAtUtc,
            HasDocument = !string.IsNullOrEmpty(p.BlobContainer) && !string.IsNullOrEmpty(p.BlobPath)
        };
    }
}

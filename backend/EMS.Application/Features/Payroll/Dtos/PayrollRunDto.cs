using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EMS.Application.Features.Payroll.Dtos
{
    public class PayrollRunDto
    {
        public Guid Id { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime ProcessedAtUtc { get; set; }
        public Guid ProcessedBy { get; set; }
        public string Status { get; set; } = null!;
        public int PayslipCount { get; set; }
        public decimal TotalNetPay { get; set; }
        public List<PayslipDto> Payslips { get; set; } = new();

        public static PayrollRunDto FromEntity(PayrollRun run)
        {
            var payslips = run.Payslips?.Select(PayslipDto.FromEntity).ToList() ?? new List<PayslipDto>();
            return new PayrollRunDto
            {
                Id = run.Id,
                PeriodStart = run.PeriodStart,
                PeriodEnd = run.PeriodEnd,
                ProcessedAtUtc = run.ProcessedAtUtc,
                ProcessedBy = run.ProcessedBy,
                Status = run.Status ?? string.Empty,
                PayslipCount = payslips.Count,
                TotalNetPay = payslips.Sum(p => p.NetPay),
                Payslips = payslips
            };
        }
    }
}

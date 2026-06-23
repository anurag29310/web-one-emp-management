using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Dtos
{
    public class SalaryStructureDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public decimal BasicSalary { get; set; }
        public List<AllowanceDto> Allowances { get; set; } = new();
        public List<DeductionDto> Deductions { get; set; } = new();
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class AllowanceDto { public Guid Id { get; set; } public string Name { get; set; } = null!; public decimal Amount { get; set; } }
    public class DeductionDto { public Guid Id { get; set; } public string Name { get; set; } = null!; public decimal Amount { get; set; } }
}

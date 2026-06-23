using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities
{
    public class SalaryStructure
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public decimal BasicSalary { get; set; }
        public System.Collections.Generic.List<Allowance>? Allowances { get; set; } = new();
        public System.Collections.Generic.List<Deduction>? Deductions { get; set; } = new();
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}

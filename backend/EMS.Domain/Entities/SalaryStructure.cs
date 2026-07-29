using System;
using System.Collections.Generic;

namespace EMS.Domain.Entities
{
    public class SalaryStructure
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public decimal BasicSalary { get; set; }
        public System.Collections.Generic.List<Allowance>? Allowances { get; set; } = new();
        public System.Collections.Generic.List<Deduction>? Deductions { get; set; } = new();
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedBy { get; set; }
        public uint RowVersion { get; set; }
    }
}

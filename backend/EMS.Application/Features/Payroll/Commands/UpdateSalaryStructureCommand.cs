using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Commands
{
    public class UpdateSalaryStructureCommand : IRequest
    {
        public Guid Id { get; set; }
        public decimal BasicSalary { get; set; }
        public List<UpdateAllowance> Allowances { get; set; } = new();
        public List<UpdateDeduction> Deductions { get; set; } = new();
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class UpdateAllowance { public Guid? Id { get; set; } public string Name { get; set; } = null!; public decimal Amount { get; set; } }
    public class UpdateDeduction { public Guid? Id { get; set; } public string Name { get; set; } = null!; public decimal Amount { get; set; } }
}

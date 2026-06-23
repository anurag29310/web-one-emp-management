using MediatR;
using System;
using System.Collections.Generic;

namespace EMS.Application.Features.Payroll.Commands
{
    public class CreateSalaryStructureCommand : IRequest<Guid>
    {
        public Guid EmployeeId { get; set; }
        public decimal BasicSalary { get; set; }
        public List<CreateAllowance> Allowances { get; set; } = new();
        public List<CreateDeduction> Deductions { get; set; } = new();
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class CreateAllowance { public string Name { get; set; } = null!; public decimal Amount { get; set; } }
    public class CreateDeduction { public string Name { get; set; } = null!; public decimal Amount { get; set; } }
}

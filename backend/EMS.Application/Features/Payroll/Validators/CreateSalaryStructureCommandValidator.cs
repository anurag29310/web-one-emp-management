using EMS.Application.Features.Payroll.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Payroll.Validators
{
    public class CreateSalaryStructureCommandValidator : AbstractValidator<CreateSalaryStructureCommand>
    {
        public CreateSalaryStructureCommandValidator(IEmployeeRepository employeeRepo)
        {
            RuleFor(x => x.EmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Employee does not exist.");
            RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);
            RuleFor(x => x.EffectiveFrom).LessThanOrEqualTo(x => x.EffectiveTo.Value).When(x => x.EffectiveTo.HasValue);
            RuleForEach(x => x.Allowances).ChildRules(a => {
                a.RuleFor(x => x.Name).NotEmpty();
                a.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
            });
            RuleForEach(x => x.Deductions).ChildRules(d => {
                d.RuleFor(x => x.Name).NotEmpty();
                d.RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
            });
        }
    }
}

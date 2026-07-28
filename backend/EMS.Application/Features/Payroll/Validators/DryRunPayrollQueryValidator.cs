using EMS.Application.Features.Payroll.Queries;
using FluentValidation;

namespace EMS.Application.Features.Payroll.Validators
{
    public class DryRunPayrollQueryValidator : AbstractValidator<DryRunPayrollQuery>
    {
        public DryRunPayrollQueryValidator()
        {
            RuleFor(x => x.PeriodStart).LessThanOrEqualTo(x => x.PeriodEnd).WithMessage("PeriodStart must be before or equal to PeriodEnd");

            RuleForEach(x => x.Adjustments).ChildRules(a =>
            {
                a.RuleFor(x => x.EmployeeId).NotEmpty();
                a.RuleFor(x => x.BonusAmount).GreaterThanOrEqualTo(0).When(x => x.BonusAmount.HasValue);
                a.RuleFor(x => x.OvertimeAmount).GreaterThanOrEqualTo(0).When(x => x.OvertimeAmount.HasValue);
            });
        }
    }
}

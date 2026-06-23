using EMS.Application.Features.Payroll.Queries;
using FluentValidation;

namespace EMS.Application.Features.Payroll.Validators
{
    public class DryRunPayrollQueryValidator : AbstractValidator<DryRunPayrollQuery>
    {
        public DryRunPayrollQueryValidator()
        {
            RuleFor(x => x.PeriodStart).LessThanOrEqualTo(x => x.PeriodEnd).WithMessage("PeriodStart must be before or equal to PeriodEnd");
        }
    }
}

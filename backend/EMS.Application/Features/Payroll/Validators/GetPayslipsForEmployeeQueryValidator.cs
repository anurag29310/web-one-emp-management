using EMS.Application.Features.Payroll.Queries;
using FluentValidation;

namespace EMS.Application.Features.Payroll.Validators
{
    public class GetPayslipsForEmployeeQueryValidator : AbstractValidator<GetPayslipsForEmployeeQuery>
    {
        public GetPayslipsForEmployeeQueryValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotNull()
                .WithMessage("The employeeId query parameter is required.")
                .When(x => x.IsPrivileged);
        }
    }
}

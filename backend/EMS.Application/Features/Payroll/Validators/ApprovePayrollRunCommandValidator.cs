using EMS.Application.Features.Payroll.Commands;
using FluentValidation;

namespace EMS.Application.Features.Payroll.Validators
{
    public class ApprovePayrollRunCommandValidator : AbstractValidator<ApprovePayrollRunCommand>
    {
        public ApprovePayrollRunCommandValidator()
        {
            RuleFor(x => x.PayrollRunId).NotEmpty();
            RuleFor(x => x.ApprovedBy).NotEmpty();
        }
    }
}

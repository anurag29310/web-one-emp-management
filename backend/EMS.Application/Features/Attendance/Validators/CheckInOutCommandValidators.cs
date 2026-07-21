using EMS.Application.Features.Attendance.Commands;
using FluentValidation;

namespace EMS.Application.Features.Attendance.Validators
{
    public class CheckInCommandValidator : AbstractValidator<CheckInCommand>
    {
        public CheckInCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.CheckInAtUtc).NotEqual(default(System.DateTime));
            RuleFor(x => x.Notes).MaximumLength(500);
        }
    }

    public class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
    {
        public CheckOutCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.CheckOutAtUtc).NotEqual(default(System.DateTime));
            RuleFor(x => x.Notes).MaximumLength(500);
        }
    }
}

using EMS.Application.Features.Attendance.Commands;
using FluentValidation;

namespace EMS.Application.Features.Attendance.Validators
{
    public class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
    {
        public CreateShiftCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.GraceMinutes).GreaterThanOrEqualTo(0);
        }
    }

    public class UpdateShiftCommandValidator : AbstractValidator<UpdateShiftCommand>
    {
        public UpdateShiftCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.GraceMinutes).GreaterThanOrEqualTo(0);
        }
    }

    public class AssignEmployeeShiftCommandValidator : AbstractValidator<AssignEmployeeShiftCommand>
    {
        public AssignEmployeeShiftCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.ShiftId).NotEmpty();
            RuleFor(x => x.EffectiveFrom).NotEqual(default(System.DateTime));
            RuleFor(x => x.EffectiveTo)
                .GreaterThanOrEqualTo(x => x.EffectiveFrom)
                .When(x => x.EffectiveTo.HasValue)
                .WithMessage("Effective-to date must be on or after the effective-from date.");
        }
    }

    public class UpdateEmployeeShiftCommandValidator : AbstractValidator<UpdateEmployeeShiftCommand>
    {
        public UpdateEmployeeShiftCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.AssignmentId).NotEmpty();
            RuleFor(x => x.ShiftId).NotEmpty();
            RuleFor(x => x.EffectiveFrom).NotEqual(default(System.DateTime));
            RuleFor(x => x.EffectiveTo)
                .GreaterThanOrEqualTo(x => x.EffectiveFrom)
                .When(x => x.EffectiveTo.HasValue)
                .WithMessage("Effective-to date must be on or after the effective-from date.");
        }
    }
}

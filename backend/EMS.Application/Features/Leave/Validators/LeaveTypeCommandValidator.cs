using EMS.Application.Features.Leave.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Leave.Validators
{
    public class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
    {
        public CreateLeaveTypeCommandValidator(ILeaveRepository repo)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Code).MaximumLength(50)
                .MustAsync(async (code, ct) => string.IsNullOrWhiteSpace(code) || !await repo.LeaveTypeCodeExistsAsync(code, null, ct))
                .WithMessage("Leave type code already exists.");

            RuleFor(x => x.AnnualEntitlementDays).GreaterThanOrEqualTo(0)
                .When(x => x.AnnualEntitlementDays.HasValue);
        }
    }

    public class UpdateLeaveTypeCommandValidator : AbstractValidator<UpdateLeaveTypeCommand>
    {
        public UpdateLeaveTypeCommandValidator(ILeaveRepository repo)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Code).MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => string.IsNullOrWhiteSpace(code) || !await repo.LeaveTypeCodeExistsAsync(code, cmd.Id, ct))
                .WithMessage("Leave type code already exists.");

            RuleFor(x => x.AnnualEntitlementDays).GreaterThanOrEqualTo(0)
                .When(x => x.AnnualEntitlementDays.HasValue);
        }
    }
}

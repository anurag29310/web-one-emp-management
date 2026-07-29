using EMS.Application.Features.Assets.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Assets.Validators
{
    public class AssignAssetCommandValidator : AbstractValidator<AssignAssetCommand>
    {
        public AssignAssetCommandValidator(IEmployeeRepository employeeRepo)
        {
            RuleFor(x => x.AssetId).NotEmpty();
            RuleFor(x => x.ConditionAtAssignment).MaximumLength(500);
            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.EmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Employee does not exist.");
        }
    }

    public class ReturnAssetCommandValidator : AbstractValidator<ReturnAssetCommand>
    {
        public ReturnAssetCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ConditionAtReturn).MaximumLength(500);
            RuleFor(x => x.Notes).MaximumLength(1000);
            RuleFor(x => x.ResultingAssetStatus).IsInEnum();
        }
    }
}

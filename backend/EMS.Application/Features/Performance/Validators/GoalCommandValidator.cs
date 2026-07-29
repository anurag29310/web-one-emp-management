using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System;

namespace EMS.Application.Features.Performance.Validators
{
    public class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
    {
        public CreateGoalCommandValidator(IEmployeeRepository employeeRepo)
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Category).MaximumLength(100);
            RuleFor(x => x.StartDate).NotEqual(default(DateTime));
            RuleFor(x => x.TargetDate).NotEqual(default(DateTime)).GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("TargetDate must be on or after StartDate.");
            RuleFor(x => x.Weight).InclusiveBetween(0, 100).When(x => x.Weight.HasValue);

            RuleFor(x => x.EmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Employee does not exist.");
        }
    }

    public class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
    {
        public UpdateGoalCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Category).MaximumLength(100);
            RuleFor(x => x.TargetDate).NotEqual(default(DateTime));
            RuleFor(x => x.Weight).InclusiveBetween(0, 100).When(x => x.Weight.HasValue);
            RuleFor(x => x.Status).IsInEnum();
        }
    }

    public class UpdateGoalProgressCommandValidator : AbstractValidator<UpdateGoalProgressCommand>
    {
        public UpdateGoalProgressCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ProgressPercent).InclusiveBetween(0, 100);
        }
    }

    public class AddGoalKpiCommandValidator : AbstractValidator<AddGoalKpiCommand>
    {
        public AddGoalKpiCommandValidator()
        {
            RuleFor(x => x.GoalId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TargetValue).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Unit).MaximumLength(30);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public class UpdateGoalKpiProgressCommandValidator : AbstractValidator<UpdateGoalKpiProgressCommand>
    {
        public UpdateGoalKpiProgressCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.CurrentValue).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }
}

using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System;

namespace EMS.Application.Features.Performance.Validators
{
    public class ProposePromotionCommandValidator : AbstractValidator<ProposePromotionCommand>
    {
        public ProposePromotionCommandValidator(IEmployeeRepository employeeRepo, IDesignationRepository designationRepo, IDepartmentRepository departmentRepo)
        {
            RuleFor(x => x.EffectiveDate).NotEqual(default(DateTime));
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);

            RuleFor(x => x.EmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Employee does not exist.");

            RuleFor(x => x.ToDesignationId).NotEmpty()
                .MustAsync(async (id, ct) => await designationRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Target designation does not exist.");

            RuleFor(x => x.ToDepartmentId)
                .MustAsync(async (id, ct) => id == null || await departmentRepo.GetByIdAsync(id.Value, ct) != null)
                .WithMessage("Target department does not exist.");
        }
    }

    public class ApprovePromotionCommandValidator : AbstractValidator<ApprovePromotionCommand>
    {
        public ApprovePromotionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.DecisionNotes).MaximumLength(1000);
        }
    }

    public class RejectPromotionCommandValidator : AbstractValidator<RejectPromotionCommand>
    {
        public RejectPromotionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.DecisionNotes).MaximumLength(1000);
        }
    }
}

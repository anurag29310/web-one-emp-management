using EMS.Application.Features.Performance.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System;

namespace EMS.Application.Features.Performance.Validators
{
    public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
    {
        public CreateReviewCommandValidator(IEmployeeRepository employeeRepo)
        {
            RuleFor(x => x.ReviewPeriodStart).NotEqual(default(DateTime));
            RuleFor(x => x.ReviewPeriodEnd).NotEqual(default(DateTime)).GreaterThanOrEqualTo(x => x.ReviewPeriodStart)
                .WithMessage("ReviewPeriodEnd must be on or after ReviewPeriodStart.");
            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.EmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Employee does not exist.");

            RuleFor(x => x.ReviewerEmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Reviewer does not exist.");

            RuleFor(x => x).Must(x => x.EmployeeId != x.ReviewerEmployeeId)
                .WithMessage("An employee cannot review themselves.");
        }
    }

    public class SubmitSelfAssessmentCommandValidator : AbstractValidator<SubmitSelfAssessmentCommand>
    {
        public SubmitSelfAssessmentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.SelfAssessment).NotEmpty().MaximumLength(4000);
        }
    }

    public class SubmitManagerReviewCommandValidator : AbstractValidator<SubmitManagerReviewCommand>
    {
        public SubmitManagerReviewCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ManagerAssessment).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.OverallRating).InclusiveBetween(1, 5);
        }
    }

    public class CancelReviewCommandValidator : AbstractValidator<CancelReviewCommand>
    {
        public CancelReviewCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }
}

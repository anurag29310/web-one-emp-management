using FluentValidation;
using System;

namespace EMS.Application.Features.Reimbursements.Validators
{
    public class CreateReimbursementCommandValidator : AbstractValidator<CreateReimbursementCommand>
    {
        public CreateReimbursementCommandValidator()
        {
            RuleFor(x => x.ExpenseTitle).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExpenseCategory).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExpenseDate).LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Expense date cannot be in the future.");
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public class UpdateReimbursementCommandValidator : AbstractValidator<UpdateReimbursementCommand>
    {
        public UpdateReimbursementCommandValidator()
        {
            RuleFor(x => x.ExpenseTitle).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExpenseCategory).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExpenseDate).LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Expense date cannot be in the future.");
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public class RejectReimbursementCommandValidator : AbstractValidator<RejectReimbursementCommand>
    {
        public RejectReimbursementCommandValidator()
        {
            RuleFor(x => x.Remarks).NotEmpty().MaximumLength(1000)
                .WithMessage("A remark explaining the rejection is required.");
        }
    }

    public class RequestChangesReimbursementCommandValidator : AbstractValidator<RequestChangesReimbursementCommand>
    {
        public RequestChangesReimbursementCommandValidator()
        {
            RuleFor(x => x.Remarks).NotEmpty().MaximumLength(1000)
                .WithMessage("A remark explaining the requested changes is required.");
        }
    }
}

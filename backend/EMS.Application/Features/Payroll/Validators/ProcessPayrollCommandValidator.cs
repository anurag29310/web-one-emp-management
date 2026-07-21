using EMS.Application.Features.Payroll.Commands;
using FluentValidation;
using System;

namespace EMS.Application.Features.Payroll.Validators
{
    public class ProcessPayrollCommandValidator : AbstractValidator<ProcessPayrollCommand>
    {
        public ProcessPayrollCommandValidator()
        {
            RuleFor(x => x.PeriodStart).NotEqual(default(DateTime)).WithMessage("PeriodStart is required.");
            RuleFor(x => x.PeriodEnd).NotEqual(default(DateTime)).WithMessage("PeriodEnd is required.");

            RuleFor(x => x.PeriodStart)
                .LessThanOrEqualTo(x => x.PeriodEnd)
                .WithMessage("PeriodStart must be before or equal to PeriodEnd.");

            RuleFor(x => x.PeriodEnd)
                .LessThanOrEqualTo(_ => DateTime.UtcNow)
                .WithMessage("Payroll cannot be processed for a period that has not yet ended.");
        }
    }
}

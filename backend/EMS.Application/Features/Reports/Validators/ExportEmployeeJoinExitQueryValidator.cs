using EMS.Application.Features.Reports.Queries;
using FluentValidation;
using System;

namespace EMS.Application.Features.Reports.Validators
{
    public class ExportEmployeeJoinExitQueryValidator : AbstractValidator<ExportEmployeeJoinExitQuery>
    {
        public ExportEmployeeJoinExitQueryValidator()
        {
            RuleFor(x => x.From).NotEqual(default(DateTime)).WithMessage("from is required.");
            RuleFor(x => x.To).NotEqual(default(DateTime)).WithMessage("to is required.");
            RuleFor(x => x.From).LessThanOrEqualTo(x => x.To).WithMessage("from must be before or equal to to.");
        }
    }
}

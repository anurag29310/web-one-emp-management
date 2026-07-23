using EMS.Application.Features.Exports.Queries;
using FluentValidation;
using System;

namespace EMS.Application.Features.Exports.Validators
{
    public class ExportDashboardSummaryQueryValidator : AbstractValidator<ExportDashboardSummaryQuery>
    {
        public ExportDashboardSummaryQueryValidator()
        {
            RuleFor(x => x.Date)
                .Must(date => !date.HasValue || date.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage("Date cannot be in the future.");
        }
    }
}

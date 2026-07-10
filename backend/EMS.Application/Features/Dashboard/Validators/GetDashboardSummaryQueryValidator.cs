using EMS.Application.Features.Dashboard.Queries;
using FluentValidation;
using System;

namespace EMS.Application.Features.Dashboard.Validators
{
    public class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
    {
        public GetDashboardSummaryQueryValidator()
        {
            RuleFor(x => x.Date)
                .Must(date => !date.HasValue || date.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage("Date cannot be in the future.");
        }
    }
}

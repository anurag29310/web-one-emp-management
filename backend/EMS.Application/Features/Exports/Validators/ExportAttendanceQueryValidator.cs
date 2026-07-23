using EMS.Application.Features.Exports.Queries;
using FluentValidation;

namespace EMS.Application.Features.Exports.Validators
{
    public class ExportAttendanceQueryValidator : AbstractValidator<ExportAttendanceQuery>
    {
        public ExportAttendanceQueryValidator()
        {
            RuleFor(x => x.DateFrom)
                .LessThanOrEqualTo(x => x.DateTo!.Value)
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
                .WithMessage("dateFrom must be before or equal to dateTo.");
        }
    }
}

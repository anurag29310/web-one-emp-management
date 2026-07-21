using EMS.Application.Features.AuditLogs.Queries;
using FluentValidation;

namespace EMS.Application.Features.AuditLogs.Validators
{
    public class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
    {
        public GetAuditLogsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.DateFrom).LessThanOrEqualTo(x => x.DateTo)
                .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
                .WithMessage("dateFrom must be before or equal to dateTo.");
        }
    }
}

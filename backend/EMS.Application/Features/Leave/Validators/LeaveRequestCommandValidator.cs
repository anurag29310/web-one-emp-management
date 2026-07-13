using EMS.Application.Features.Leave.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Leave.Validators
{
    public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
    {
        public CreateLeaveRequestCommandValidator(ILeaveRepository repo)
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.LeaveTypeId).NotEmpty();
            RuleFor(x => x.StartDate).NotEqual(default(System.DateTime));
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be on or after start date.");
            RuleFor(x => x.TotalDays).GreaterThan(0);

            RuleFor(x => x.TotalDays)
                .MustAsync(async (cmd, totalDays, ct) =>
                {
                    var balance = await repo.GetLeaveBalanceAsync(cmd.EmployeeId, cmd.LeaveTypeId, cmd.StartDate.Year, ct);
                    return balance == null || totalDays <= balance.Available;
                })
                .WithMessage("Requested days exceed the available leave balance.");
        }
    }

    public class UpdateLeaveRequestCommandValidator : AbstractValidator<UpdateLeaveRequestCommand>
    {
        public UpdateLeaveRequestCommandValidator()
        {
            RuleFor(x => x.StartDate).NotEqual(default(System.DateTime));
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be on or after start date.");
            RuleFor(x => x.TotalDays).GreaterThan(0);
        }
    }

    public class AdjustLeaveBalanceCommandValidator : AbstractValidator<AdjustLeaveBalanceCommand>
    {
        public AdjustLeaveBalanceCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.LeaveTypeId).NotEmpty();
            RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        }
    }
}

using EMS.Application.Features.Attendance.Commands;
using FluentValidation;

namespace EMS.Application.Features.Attendance.Validators
{
    public class CreateAttendanceCorrectionCommandValidator : AbstractValidator<CreateAttendanceCorrectionCommand>
    {
        public CreateAttendanceCorrectionCommandValidator()
        {
            RuleFor(x => x.AttendanceRecordId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
            RuleFor(x => x)
                .Must(x => x.RequestedCheckInAtUtc.HasValue || x.RequestedCheckOutAtUtc.HasValue)
                .WithMessage("At least one of requestedCheckInAtUtc or requestedCheckOutAtUtc must be provided.");
        }
    }
}

using EMS.Application.Features.Attendance.Commands;
using EMS.Application.Interfaces;
using EMS.Domain.Enums;
using FluentValidation;

namespace EMS.Application.Features.Attendance.Validators
{
    public class CreateAttendanceRecordCommandValidator : AbstractValidator<CreateAttendanceRecordCommand>
    {
        public CreateAttendanceRecordCommandValidator(IAttendanceRepository repo)
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.AttendanceDate).NotEqual(default(System.DateTime));
            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(s => System.Enum.TryParse<AttendanceStatus>(s, true, out _))
                .WithMessage("Status must be one of: Present, Absent, Late, HalfDay, OnLeave, Holiday.");
            RuleFor(x => x.Notes).MaximumLength(500);

            RuleFor(x => x.ShiftId)
                .MustAsync(async (shiftId, ct) => !shiftId.HasValue || await repo.GetShiftByIdAsync(shiftId.Value, ct) != null)
                .WithMessage("Shift not found.");

            RuleFor(x => x.CheckOutAtUtc)
                .GreaterThanOrEqualTo(x => x.CheckInAtUtc!.Value)
                .When(x => x.CheckInAtUtc.HasValue && x.CheckOutAtUtc.HasValue)
                .WithMessage("Check-out time must be on or after check-in time.");
        }
    }

    public class UpdateAttendanceRecordCommandValidator : AbstractValidator<UpdateAttendanceRecordCommand>
    {
        public UpdateAttendanceRecordCommandValidator(IAttendanceRepository repo)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(s => System.Enum.TryParse<AttendanceStatus>(s, true, out _))
                .WithMessage("Status must be one of: Present, Absent, Late, HalfDay, OnLeave, Holiday.");
            RuleFor(x => x.Notes).MaximumLength(500);

            RuleFor(x => x.ShiftId)
                .MustAsync(async (shiftId, ct) => !shiftId.HasValue || await repo.GetShiftByIdAsync(shiftId.Value, ct) != null)
                .WithMessage("Shift not found.");

            RuleFor(x => x.CheckOutAtUtc)
                .GreaterThanOrEqualTo(x => x.CheckInAtUtc!.Value)
                .When(x => x.CheckInAtUtc.HasValue && x.CheckOutAtUtc.HasValue)
                .WithMessage("Check-out time must be on or after check-in time.");
        }
    }
}

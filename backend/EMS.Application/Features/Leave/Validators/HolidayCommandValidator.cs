using EMS.Application.Features.Leave.Commands;
using FluentValidation;

namespace EMS.Application.Features.Leave.Validators
{
    public class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
    {
        public CreateHolidayCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.HolidayDate).NotEqual(default(System.DateTime));
        }
    }

    public class UpdateHolidayCommandValidator : AbstractValidator<UpdateHolidayCommand>
    {
        public UpdateHolidayCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.HolidayDate).NotEqual(default(System.DateTime));
        }
    }
}

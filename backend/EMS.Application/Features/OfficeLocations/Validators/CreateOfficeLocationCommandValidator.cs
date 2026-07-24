using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.OfficeLocations.Validators
{
    public class CreateOfficeLocationCommandValidator : AbstractValidator<CreateOfficeLocationCommand>
    {
        public CreateOfficeLocationCommandValidator(IOfficeLocationRepository repo)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

            RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
                .MustAsync(async (code, ct) => !await repo.CodeExistsAsync(code, null, ct)).WithMessage("Office location code already exists.");

            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AddressLine1).MaximumLength(250);
            RuleFor(x => x.AddressLine2).MaximumLength(250);
            RuleFor(x => x.State).MaximumLength(100);
        }
    }

    public class UpdateOfficeLocationCommandValidator : AbstractValidator<UpdateOfficeLocationCommand>
    {
        public UpdateOfficeLocationCommandValidator(IOfficeLocationRepository repo)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

            RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => !await repo.CodeExistsAsync(code, cmd.Id, ct)).WithMessage("Office location code already exists.");

            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AddressLine1).MaximumLength(250);
            RuleFor(x => x.AddressLine2).MaximumLength(250);
            RuleFor(x => x.State).MaximumLength(100);
        }
    }
}

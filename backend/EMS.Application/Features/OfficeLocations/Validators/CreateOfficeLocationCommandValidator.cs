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

            RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
            RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
            RuleFor(x => x.GeofenceRadiusMeters).GreaterThan(0).When(x => x.GeofenceRadiusMeters.HasValue);
            RuleFor(x => x)
                .Must(x => (x.Latitude.HasValue && x.Longitude.HasValue && x.GeofenceRadiusMeters.HasValue)
                    || (!x.Latitude.HasValue && !x.Longitude.HasValue && !x.GeofenceRadiusMeters.HasValue))
                .WithMessage("Latitude, Longitude, and GeofenceRadiusMeters must all be set together, or all left empty.");
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

            RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
            RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
            RuleFor(x => x.GeofenceRadiusMeters).GreaterThan(0).When(x => x.GeofenceRadiusMeters.HasValue);
            RuleFor(x => x)
                .Must(x => (x.Latitude.HasValue && x.Longitude.HasValue && x.GeofenceRadiusMeters.HasValue)
                    || (!x.Latitude.HasValue && !x.Longitude.HasValue && !x.GeofenceRadiusMeters.HasValue))
                .WithMessage("Latitude, Longitude, and GeofenceRadiusMeters must all be set together, or all left empty.");
        }
    }
}

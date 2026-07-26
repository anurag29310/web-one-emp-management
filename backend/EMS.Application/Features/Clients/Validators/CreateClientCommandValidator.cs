using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Clients.Validators
{
    public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
    {
        public CreateClientCommandValidator(IClientRepository repo)
        {
            RuleFor(x => x.ClientName).NotEmpty().MaximumLength(150)
                .MustAsync(async (name, ct) => !await repo.NameExistsAsync(name, null, ct)).WithMessage("Client name already exists.");

            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.ContactPerson).NotEmpty().MaximumLength(150);
            RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.AlternateMobile).MaximumLength(20);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
            RuleFor(x => x.GstNumber).MaximumLength(20);
            RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(250);
            RuleFor(x => x.AddressLine2).MaximumLength(250);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.State).MaximumLength(100);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
    {
        public UpdateClientCommandValidator(IClientRepository repo)
        {
            RuleFor(x => x.ClientName).NotEmpty().MaximumLength(150)
                .MustAsync(async (cmd, name, ct) => !await repo.NameExistsAsync(name, cmd.Id, ct)).WithMessage("Client name already exists.");

            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.ContactPerson).NotEmpty().MaximumLength(150);
            RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(20);
            RuleFor(x => x.AlternateMobile).MaximumLength(20);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
            RuleFor(x => x.GstNumber).MaximumLength(20);
            RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(250);
            RuleFor(x => x.AddressLine2).MaximumLength(250);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.State).MaximumLength(100);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }
}

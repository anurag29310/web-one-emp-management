using EMS.Application.Features.Companies.Commands;
using FluentValidation;

namespace EMS.Application.Features.Companies.Validators
{
    public class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
    {
        public RegisterCompanyCommandValidator()
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);

            RuleFor(x => x.AdminUserName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8);
        }
    }
}

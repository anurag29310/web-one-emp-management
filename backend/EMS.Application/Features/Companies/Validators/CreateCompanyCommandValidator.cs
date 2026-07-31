using EMS.Application.Features.Companies.Commands;
using FluentValidation;

namespace EMS.Application.Features.Companies.Validators
{
    public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
    {
        public CreateCompanyCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.LogoUrl).MaximumLength(500);
        }
    }

    public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
    {
        public UpdateCompanyCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Timezone).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.LogoUrl).MaximumLength(500);
        }
    }

    public class SuspendCompanyCommandValidator : AbstractValidator<SuspendCompanyCommand>
    {
        public SuspendCompanyCommandValidator()
        {
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }
}

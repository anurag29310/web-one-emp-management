using EMS.Application.Features.Assets.Commands;
using FluentValidation;

namespace EMS.Application.Features.Assets.Validators
{
    public class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
    {
        public CreateAssetCommandValidator()
        {
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Brand).MaximumLength(100);
            RuleFor(x => x.Model).MaximumLength(100);
            RuleFor(x => x.SerialNumber).MaximumLength(150);
            RuleFor(x => x.PurchaseCost).GreaterThanOrEqualTo(0).When(x => x.PurchaseCost.HasValue);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public class UpdateAssetCommandValidator : AbstractValidator<UpdateAssetCommand>
    {
        public UpdateAssetCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Brand).MaximumLength(100);
            RuleFor(x => x.Model).MaximumLength(100);
            RuleFor(x => x.SerialNumber).MaximumLength(150);
            RuleFor(x => x.PurchaseCost).GreaterThanOrEqualTo(0).When(x => x.PurchaseCost.HasValue);
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }
}

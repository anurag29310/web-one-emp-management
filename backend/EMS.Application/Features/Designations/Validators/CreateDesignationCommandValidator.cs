using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Designations.Validators
{
    public class CreateDesignationCommandValidator : AbstractValidator<CreateDesignationCommand>
    {
        public CreateDesignationCommandValidator(IDesignationRepository repo, ICurrentUserService currentUser)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150)
                .MustAsync(async (name, ct) => !await repo.NameExistsAsync(name, currentUser.CompanyId!.Value, null, ct)).WithMessage("Designation name already exists.");

            RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
                .MustAsync(async (code, ct) => !await repo.CodeExistsAsync(code, currentUser.CompanyId!.Value, null, ct)).WithMessage("Designation code already exists.");
        }
    }

    public class UpdateDesignationCommandValidator : AbstractValidator<UpdateDesignationCommand>
    {
        public UpdateDesignationCommandValidator(IDesignationRepository repo, ICurrentUserService currentUser)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150)
                .MustAsync(async (cmd, name, ct) => !await repo.NameExistsAsync(name, currentUser.CompanyId!.Value, cmd.Id, ct)).WithMessage("Designation name already exists.");

            RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => !await repo.CodeExistsAsync(code, currentUser.CompanyId!.Value, cmd.Id, ct)).WithMessage("Designation code already exists.");
        }
    }
}

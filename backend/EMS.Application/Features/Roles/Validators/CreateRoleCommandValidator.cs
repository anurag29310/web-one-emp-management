using EMS.Application.Features.Roles.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Roles.Validators
{
    public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidator(IRoleRepository repo)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50)
                .MustAsync(async (name, ct) => !await repo.NameExistsAsync(name, null, ct))
                .WithMessage("Role name already exists.");

            RuleFor(x => x.Description).MaximumLength(250);
        }
    }

    public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidator(IRoleRepository repo)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, name, ct) => !await repo.NameExistsAsync(name, cmd.Id, ct))
                .WithMessage("Role name already exists.");

            RuleFor(x => x.Description).MaximumLength(250);
        }
    }
}

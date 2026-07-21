using EMS.Application.Features.Users.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Users.Validators
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator(IUserRepository users, IRoleRepository roles)
        {
            RuleFor(x => x.UserName).NotEmpty().MaximumLength(256)
                .MustAsync(async (cmd, userName, ct) => !await users.UserNameExistsAsync(userName, cmd.Id, ct))
                .WithMessage("Username already exists.");

            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256)
                .MustAsync(async (cmd, email, ct) => !await users.EmailExistsAsync(email, cmd.Id, ct))
                .WithMessage("Email already exists.");

            RuleFor(x => x.RoleId)
                .MustAsync(async (roleId, ct) => roleId == null || await roles.GetByIdAsync(roleId.Value, ct) != null)
                .WithMessage("Role not found.");
        }
    }
}

using EMS.Application.Features.Users.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Users.Validators
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator(IUserRepository users, IRoleRepository roles)
        {
            RuleFor(x => x.UserName).NotEmpty().MaximumLength(256)
                .MustAsync(async (userName, ct) => !await users.UserNameExistsAsync(userName, null, ct))
                .WithMessage("Username already exists.");

            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256)
                .MustAsync(async (email, ct) => !await users.EmailExistsAsync(email, null, ct))
                .WithMessage("Email already exists.");

            RuleFor(x => x.TemporaryPassword)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Temporary password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Temporary password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Temporary password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Temporary password must contain at least one special character.");

            RuleFor(x => x.RoleId)
                .MustAsync(async (roleId, ct) => roleId == null || await roles.GetByIdAsync(roleId.Value, ct) != null)
                .WithMessage("Role not found.");
        }
    }
}

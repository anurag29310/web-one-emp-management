using FluentValidation;

namespace EMS.Application.Features.Auth.Validators
{
    public class LoginCommandValidator : AbstractValidator<global::EMS.Application.Features.Auth.LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.UserNameOrEmail).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }
}

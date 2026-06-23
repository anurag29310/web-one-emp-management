using FluentValidation;

namespace EMS.Application.Features.Departments.Validators
{
    public class CreateDepartmentCommandValidator : AbstractValidator<global::EMS.Application.Features.Departments.CreateDepartmentCommand>
    {
        public CreateDepartmentCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Code).MaximumLength(50);
        }
    }
}

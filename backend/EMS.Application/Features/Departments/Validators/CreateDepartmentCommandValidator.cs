using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Departments.Validators
{
    public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
    {
        public CreateDepartmentCommandValidator(IDepartmentRepository repo, ICurrentUserService currentUser)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150)
                .MustAsync(async (name, ct) => !await repo.NameExistsAsync(name, currentUser.CompanyId!.Value, null, ct)).WithMessage("Department name already exists.");

            RuleFor(x => x.Code).MaximumLength(50)
                .MustAsync(async (code, ct) => string.IsNullOrWhiteSpace(code) || !await repo.CodeExistsAsync(code, currentUser.CompanyId!.Value, null, ct))
                .WithMessage("Department code already exists.");

            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
    {
        public UpdateDepartmentCommandValidator(IDepartmentRepository repo, ICurrentUserService currentUser)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150)
                .MustAsync(async (cmd, name, ct) => !await repo.NameExistsAsync(name, currentUser.CompanyId!.Value, cmd.Id, ct)).WithMessage("Department name already exists.");

            RuleFor(x => x.Code).MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => string.IsNullOrWhiteSpace(code) || !await repo.CodeExistsAsync(code, currentUser.CompanyId!.Value, cmd.Id, ct))
                .WithMessage("Department code already exists.");

            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}

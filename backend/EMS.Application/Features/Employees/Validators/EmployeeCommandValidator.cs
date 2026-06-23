using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Validators
{
    public class EmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public EmployeeCommandValidator(IEmployeeRepository repo)
        {
            RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50)
                .MustAsync(async (code, ct) => !await repo.EmployeeCodeExistsAsync(code, null)).WithMessage("Employee code already exists");

            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .MustAsync(async (email, ct) => !await repo.EmailExistsAsync(email, null)).WithMessage("Email already exists");
        }
    }

    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator(IEmployeeRepository repo)
        {
            RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => !await repo.EmployeeCodeExistsAsync(code, cmd.Id)).WithMessage("Employee code already exists");

            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .MustAsync(async (cmd, email, ct) => !await repo.EmailExistsAsync(email, cmd.Id)).WithMessage("Email already exists");
        }
    }
}

using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Teams.Validators
{
    public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
    {
        public CreateTeamCommandValidator(ITeamRepository repo, IDepartmentRepository departmentRepo)
        {
            RuleFor(x => x.DepartmentId).NotEmpty()
                .MustAsync(async (id, ct) => await departmentRepo.GetByIdAsync(id, ct) != null).WithMessage("Department does not exist.");

            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

            RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => !await repo.CodeExistsAsync(cmd.DepartmentId, code, null, ct))
                .WithMessage("Team code already exists in this department.");
        }
    }

    public class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
    {
        public UpdateTeamCommandValidator(ITeamRepository repo, IDepartmentRepository departmentRepo)
        {
            RuleFor(x => x.DepartmentId).NotEmpty()
                .MustAsync(async (id, ct) => await departmentRepo.GetByIdAsync(id, ct) != null).WithMessage("Department does not exist.");

            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);

            RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => !await repo.CodeExistsAsync(cmd.DepartmentId, code, cmd.Id, ct))
                .WithMessage("Team code already exists in this department.");
        }
    }
}

using EMS.Application.Features.Employees.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;

namespace EMS.Application.Features.Employees.Validators
{
    public class EmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public EmployeeCommandValidator(
            IEmployeeRepository repo,
            ITeamRepository teamRepo,
            IDesignationRepository designationRepo,
            IOfficeLocationRepository officeLocationRepo)
        {
            RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50)
                .MustAsync(async (code, ct) => !await repo.EmployeeCodeExistsAsync(code, null)).WithMessage("Employee code already exists");

            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .MustAsync(async (email, ct) => !await repo.EmailExistsAsync(email, null)).WithMessage("Email already exists");

            RuleFor(x => x.DesignationId).NotEmpty()
                .MustAsync(async (id, ct) => await designationRepo.GetByIdAsync(id, ct) != null).WithMessage("Designation does not exist");

            RuleFor(x => x.OfficeLocationId).NotEmpty()
                .MustAsync(async (id, ct) => await officeLocationRepo.GetByIdAsync(id, ct) != null).WithMessage("Office location does not exist");

            RuleFor(x => x.TeamId)
                .MustAsync(async (cmd, teamId, ct) =>
                {
                    if (teamId == null) return true;
                    var team = await teamRepo.GetByIdAsync(teamId.Value, ct);
                    return team != null && (cmd.DepartmentId == null || team.DepartmentId == cmd.DepartmentId);
                })
                .WithMessage("Team does not exist or does not belong to the selected department")
                .When(x => x.TeamId != null);
        }
    }

    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator(
            IEmployeeRepository repo,
            ITeamRepository teamRepo,
            IDesignationRepository designationRepo,
            IOfficeLocationRepository officeLocationRepo)
        {
            RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50)
                .MustAsync(async (cmd, code, ct) => !await repo.EmployeeCodeExistsAsync(code, cmd.Id)).WithMessage("Employee code already exists");

            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
                .MustAsync(async (cmd, email, ct) => !await repo.EmailExistsAsync(email, cmd.Id)).WithMessage("Email already exists");

            RuleFor(x => x.DesignationId).NotEmpty()
                .MustAsync(async (id, ct) => await designationRepo.GetByIdAsync(id, ct) != null).WithMessage("Designation does not exist");

            RuleFor(x => x.OfficeLocationId).NotEmpty()
                .MustAsync(async (id, ct) => await officeLocationRepo.GetByIdAsync(id, ct) != null).WithMessage("Office location does not exist");

            RuleFor(x => x.TeamId)
                .MustAsync(async (cmd, teamId, ct) =>
                {
                    if (teamId == null) return true;
                    var team = await teamRepo.GetByIdAsync(teamId.Value, ct);
                    return team != null && (cmd.DepartmentId == null || team.DepartmentId == cmd.DepartmentId);
                })
                .WithMessage("Team does not exist or does not belong to the selected department")
                .When(x => x.TeamId != null);
        }
    }
}

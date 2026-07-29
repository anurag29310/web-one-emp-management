using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Recruitment.Validators
{
    public class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
    {
        public CreateCandidateCommandValidator(IDesignationRepository designationRepo, IDepartmentRepository departmentRepo)
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.PhoneNumber).MaximumLength(30);
            RuleFor(x => x.Source).MaximumLength(100);
            RuleFor(x => x.Notes).MaximumLength(1000);
            RuleFor(x => x.AppliedDate).NotEqual(default(System.DateTime));

            RuleFor(x => x.DesignationId).NotEmpty()
                .MustAsync(async (id, ct) => await designationRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Designation does not exist.");

            RuleFor(x => x.DepartmentId)
                .MustAsync(async (id, ct) => id == null || await departmentRepo.GetByIdAsync(id.Value, ct) != null)
                .WithMessage("Department does not exist.");
        }
    }

    public class UpdateCandidateCommandValidator : AbstractValidator<UpdateCandidateCommand>
    {
        public UpdateCandidateCommandValidator(IDesignationRepository designationRepo, IDepartmentRepository departmentRepo)
        {
            RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.PhoneNumber).MaximumLength(30);
            RuleFor(x => x.Source).MaximumLength(100);
            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.DesignationId).NotEmpty()
                .MustAsync(async (id, ct) => await designationRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Designation does not exist.");

            RuleFor(x => x.DepartmentId)
                .MustAsync(async (id, ct) => id == null || await departmentRepo.GetByIdAsync(id.Value, ct) != null)
                .WithMessage("Department does not exist.");
        }
    }
}

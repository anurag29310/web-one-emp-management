using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System;

namespace EMS.Application.Features.Recruitment.Validators
{
    public class CreateOfferCommandValidator : AbstractValidator<CreateOfferCommand>
    {
        public CreateOfferCommandValidator(IDesignationRepository designationRepo, IDepartmentRepository departmentRepo, ICurrentUserService currentUser)
        {
            RuleFor(x => x.CandidateId).NotEmpty();
            RuleFor(x => x.OfferedSalary).GreaterThan(0);
            RuleFor(x => x.JoiningDate).NotEqual(default(DateTime));
            RuleFor(x => x.ExpiresAtUtc).GreaterThan(DateTime.UtcNow).When(x => x.ExpiresAtUtc.HasValue);
            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.DesignationId).NotEmpty()
                .MustAsync(async (id, ct) => await designationRepo.GetByIdAsync(id, currentUser.CompanyId!.Value, ct) != null)
                .WithMessage("Designation does not exist.");

            RuleFor(x => x.DepartmentId)
                .MustAsync(async (id, ct) => id == null || await departmentRepo.GetByIdAsync(id.Value, currentUser.CompanyId!.Value, ct) != null)
                .WithMessage("Department does not exist.");
        }
    }

    public class RejectOfferCommandValidator : AbstractValidator<RejectOfferCommand>
    {
        public RejectOfferCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }
}

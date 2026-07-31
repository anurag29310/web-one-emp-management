using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Recruitment.Validators
{
    public class AddChecklistItemCommandValidator : AbstractValidator<AddChecklistItemCommand>
    {
        public AddChecklistItemCommandValidator()
        {
            RuleFor(x => x.CandidateId).NotEmpty();
            RuleFor(x => x.ItemName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Notes).MaximumLength(500);
        }
    }

    public class ConvertCandidateToEmployeeCommandValidator : AbstractValidator<ConvertCandidateToEmployeeCommand>
    {
        public ConvertCandidateToEmployeeCommandValidator(IOfficeLocationRepository officeLocationRepo, ICurrentUserService currentUser)
        {
            RuleFor(x => x.CandidateId).NotEmpty();
            RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);

            RuleFor(x => x.OfficeLocationId).NotEmpty()
                .MustAsync(async (id, ct) => await officeLocationRepo.GetByIdAsync(id, currentUser.CompanyId!.Value, ct) != null)
                .WithMessage("Office location does not exist.");
        }
    }
}

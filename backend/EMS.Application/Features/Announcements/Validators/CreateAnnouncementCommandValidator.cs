using EMS.Application.Features.Announcements.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System;
using System.Linq;

namespace EMS.Application.Features.Announcements.Validators
{
    public class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
    {
        private static readonly string[] ValidAudienceTypes = { "All", "Department", "Role" };
        private static readonly string[] ValidPriorities = { "Normal", "Important", "Critical" };

        public CreateAnnouncementCommandValidator(IDepartmentRepository departmentRepo)
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
            RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p))
                .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");
            RuleFor(x => x.AudienceType).Must(a => ValidAudienceTypes.Contains(a))
                .WithMessage($"AudienceType must be one of: {string.Join(", ", ValidAudienceTypes)}.");

            RuleFor(x => x.DepartmentId)
                .NotNull().WithMessage("DepartmentId is required when AudienceType is Department.")
                .MustAsync(async (id, ct) => id == null || await departmentRepo.GetByIdAsync(id.Value, ct) != null)
                .WithMessage("DepartmentId does not refer to an existing department.")
                .When(x => x.AudienceType == "Department");

            RuleFor(x => x.TargetRole).NotEmpty()
                .WithMessage("TargetRole is required when AudienceType is Role.")
                .When(x => x.AudienceType == "Role");

            RuleFor(x => x.ExpiresAtUtc)
                .GreaterThan(_ => DateTime.UtcNow)
                .WithMessage("ExpiresAtUtc must be in the future.")
                .When(x => x.ExpiresAtUtc != null);
        }
    }
}

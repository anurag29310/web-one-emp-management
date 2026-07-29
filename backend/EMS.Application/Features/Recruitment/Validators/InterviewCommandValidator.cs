using EMS.Application.Features.Recruitment.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System;

namespace EMS.Application.Features.Recruitment.Validators
{
    public class ScheduleInterviewCommandValidator : AbstractValidator<ScheduleInterviewCommand>
    {
        public ScheduleInterviewCommandValidator(IEmployeeRepository employeeRepo)
        {
            RuleFor(x => x.CandidateId).NotEmpty();
            RuleFor(x => x.Round).NotEmpty().MaximumLength(150);
            RuleFor(x => x.ScheduledAtUtc).NotEqual(default(DateTime));
            RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);

            RuleFor(x => x.InterviewerEmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Interviewer does not exist.");
        }
    }

    public class RescheduleInterviewCommandValidator : AbstractValidator<RescheduleInterviewCommand>
    {
        public RescheduleInterviewCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ScheduledAtUtc).NotEqual(default(DateTime));
            RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        }
    }

    public class SubmitInterviewFeedbackCommandValidator : AbstractValidator<SubmitInterviewFeedbackCommand>
    {
        public SubmitInterviewFeedbackCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Feedback).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Rating).InclusiveBetween(1, 5);
            RuleFor(x => x.Outcome).IsInEnum().NotEqual(EMS.Domain.Enums.InterviewOutcome.Pending)
                .WithMessage("Outcome must be Passed, Failed, or OnHold.");
        }
    }
}

using EMS.Application.Features.Messaging.Commands;
using EMS.Application.Interfaces;
using FluentValidation;
using System.Linq;

namespace EMS.Application.Features.Messaging.Validators
{
    public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
    {
        public CreateConversationCommandValidator(IUserRepository userRepo)
        {
            RuleFor(x => x.InitialMessageBody).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.Title).MaximumLength(250);

            RuleFor(x => x.ParticipantUserIds)
                .Must(ids => ids != null && ids.Any())
                .WithMessage("At least one other participant is required.");

            RuleForEach(x => x.ParticipantUserIds)
                .MustAsync(async (id, ct) =>
                {
                    var user = await userRepo.GetByIdAsync(id, ct);
                    return user != null && user.IsActive;
                })
                .WithMessage("Participant user does not exist or is inactive.");
        }
    }

    public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
    {
        public SendMessageCommandValidator()
        {
            RuleFor(x => x.ConversationId).NotEmpty();
            RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        }
    }

    public class AddParticipantsCommandValidator : AbstractValidator<AddParticipantsCommand>
    {
        public AddParticipantsCommandValidator(IUserRepository userRepo)
        {
            RuleFor(x => x.ConversationId).NotEmpty();

            RuleFor(x => x.UserIds)
                .Must(ids => ids != null && ids.Any())
                .WithMessage("At least one user id is required.");

            RuleForEach(x => x.UserIds)
                .MustAsync(async (id, ct) =>
                {
                    var user = await userRepo.GetByIdAsync(id, ct);
                    return user != null && user.IsActive;
                })
                .WithMessage("User does not exist or is inactive.");
        }
    }
}

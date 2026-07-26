using EMS.Application.Interfaces;
using FluentValidation;

namespace EMS.Application.Features.Tasks.Validators
{
    public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator(IEmployeeRepository employeeRepo, IClientRepository clientRepo)
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.AssignedEmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Assigned employee does not exist.");

            RuleFor(x => x.ClientId)
                .MustAsync(async (id, ct) =>
                {
                    if (id == null) return true;
                    var client = await clientRepo.GetByIdAsync(id.Value, ct);
                    return client is { IsActive: true };
                })
                .WithMessage("Client does not exist or is inactive — inactive clients cannot receive new tasks.")
                .When(x => x.ClientId.HasValue);

            RuleFor(x => x.DueDate)
                .GreaterThanOrEqualTo(System.DateTime.UtcNow.Date)
                .WithMessage("Due date cannot be in the past.")
                .When(x => x.DueDate.HasValue);
        }
    }

    public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
    {
        public UpdateTaskCommandValidator(IClientRepository clientRepo)
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.ClientId)
                .MustAsync(async (id, ct) =>
                {
                    if (id == null) return true;
                    var client = await clientRepo.GetByIdAsync(id.Value, ct);
                    return client is { IsActive: true };
                })
                .WithMessage("Client does not exist or is inactive — inactive clients cannot receive new tasks.")
                .When(x => x.ClientId.HasValue);
        }
    }

    public class ReassignTaskCommandValidator : AbstractValidator<ReassignTaskCommand>
    {
        public ReassignTaskCommandValidator(IEmployeeRepository employeeRepo)
        {
            RuleFor(x => x.AssignedEmployeeId).NotEmpty()
                .MustAsync(async (id, ct) => await employeeRepo.GetByIdAsync(id, ct) != null)
                .WithMessage("Assigned employee does not exist.");
        }
    }

    public class UpdateTaskProgressCommandValidator : AbstractValidator<UpdateTaskProgressCommand>
    {
        public UpdateTaskProgressCommandValidator()
        {
            RuleFor(x => x.Status)
                .Must(s => s is EMS.Domain.Enums.TaskItemStatus.InProgress or EMS.Domain.Enums.TaskItemStatus.OnHold)
                .WithMessage("Progress updates may only set status to InProgress or OnHold.");
        }
    }

    public class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
    {
        public AddTaskCommentCommandValidator()
        {
            RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
        }
    }
}
